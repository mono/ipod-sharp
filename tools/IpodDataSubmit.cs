using System;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Threading;
using System.Collections;
using System.Xml.Serialization;

using Mono.Unix;
using Gtk;
using IPod;

namespace IPod.DataSubmit
{
    public static class EntryPoint
    {
        public static void Main(string [] args)
        {
            Application.Init();
            Device device;
            
            try {
                device = new Device(args[0]);
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                return;
            }

            if (device.SerialNumber != null &&
                File.Exists (String.Format ("{0}/.ipod-data-submit-{1}", Environment.GetEnvironmentVariable ("HOME"),
                                            device.SerialNumber))) {
                return;
            }
            
            DataCollectWindow win = new DataCollectWindow(device);
            win.DeleteEvent += delegate { Application.Quit(); };
            win.Show();
            Application.Run();
        }
    }

    public class DataCollectWindow : Gtk.Window
    {
        private ProgressBar progress_bar;
        private bool working = false;
        private uint timeout_id;
        private TreeStore model_store;
        private ComboBox combo_box;
        private Device device;
        private Entry model_entry;
        private CheckButton dont_ask_button;

        public DataCollectWindow(Device device) : base(Catalog.GetString("iPod Data Collector"))
        {
            this.device = device;
        
            BuildWidget();
            LoadModels();
        }
        
        private void BuildWidget()
        {
            BorderWidth = 15;
            IconName = "music-player-banshee";
            WindowPosition = WindowPosition.Center;
            
            VBox box = new VBox();
            box.Spacing = 10;
            
            Label header = new Label();
            header.Xalign = 0.0f;
            header.Markup = String.Format("<big><b>{0}</b></big>", GLib.Markup.EscapeText(
                Catalog.GetString("Submit your iPod")));
                
            HBox hbox = new HBox();
            hbox.Spacing = 10;
            
            Label message = new Label();
            message.Xalign = 0.0f;
            message.Markup = Catalog.GetString(
                "Your iPod could not be identified.\nPlease submit the following.");
            message.Wrap = true;
            
            Button learn_more = new Button(Catalog.GetString("More information..."));
            learn_more.Clicked += delegate {
                Gnome.Url.Show("http://banshee-project.org/SubmitMyIpod");
            };
            
            hbox.PackStart(message, true, true, 0);
            hbox.PackStart(learn_more, false, false, 0);
            
            progress_bar = new ProgressBar();
            
            Table table = new Table(1, 2, false);
            table.BorderWidth = 10;
            table.ColumnSpacing = 10;
            table.RowSpacing = 3;
            
            Label model_label = new Label();
            model_label.Xalign = 0.0f;
            model_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText("Select model:"));
            
            table.Attach(model_label, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            
            combo_box = new ComboBox();
            CellRendererText renderer = new CellRendererText();
            combo_box.PackStart(renderer, true);
            combo_box.SetAttributes(renderer, "text", 0);
             
            table.Attach(combo_box, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Fill | AttachOptions.Expand, 0, 0);
             
            Label modelnum_label = new Label();
            modelnum_label.Xalign = 0.0f;
            modelnum_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText("Enter Model Number:"));
            
            table.Attach(modelnum_label, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            
            model_entry = new Entry();
            
            table.Attach(model_entry, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Fill | AttachOptions.Expand, 0, 0);
             
            Label serial_label = new Label();
            serial_label.Xalign = 0.0f;
            serial_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText("Serial Number:"));
            
            table.Attach(serial_label, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            
            Entry serial_label_value = new Entry();
            serial_label_value.Text = device.SerialNumber == null ? "Unknown" : device.SerialNumber;
            serial_label_value.IsEditable = false;
            
            table.Attach(serial_label_value, 1, 2, 2, 3, AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Fill | AttachOptions.Expand, 0, 0);

            dont_ask_button = new CheckButton ("Don't show this dialog again for this device");
            table.Attach (dont_ask_button, 1, 2, 3, 4, AttachOptions.Fill | AttachOptions.Expand,
                          AttachOptions.Fill | AttachOptions.Expand, 0, 0);
                
            HButtonBox buttons = new HButtonBox();
            buttons.Spacing = 5;
            buttons.Layout = ButtonBoxStyle.End;
            
            Button cancel_button = new Button(Stock.Cancel);
            cancel_button.Clicked += delegate {
                WriteNoAskFile ();
                Application.Quit();
            };
            
            cancel_button.UseStock = true;
            
            Button submit_button = new Button(Catalog.GetString("Submit iPod Data"));
            submit_button.Clicked += delegate { SubmitData(); };
            
            buttons.PackStart(cancel_button, false, false, 0);
            buttons.PackStart(submit_button, false, false, 0);
                
            box.PackStart(header, false, false, 0);
            box.PackStart(hbox, false, false, 0);
            box.PackStart(progress_bar, false, false, 0);
            box.PackStart(table, false, false, 0);
            box.PackStart(buttons, false, false, 0);
            
            Add(box);
            
            box.ShowAll();
            progress_bar.Hide();
            
            combo_box.Changed += delegate { ConfigureGeometry(); };
            Realized += delegate { ConfigureGeometry(); };
        }
        
        private void ConfigureGeometry()
        {
            Gdk.Geometry limits = new Gdk.Geometry();
            
            limits.MinWidth = SizeRequest().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            
            SetGeometryHints(this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }
        
        private void BeginWork()
        {
            StopWork();
            
            while(working);
            
            lock(this) {
                working = true;
                timeout_id = GLib.Timeout.Add(100, Pulse);
            }
        }
        
        private void StopWork()
        {
            lock(this) {
                working = false;
            }
        }
        
        private bool Pulse()
        {
            lock(this) {
                if(!working) {
                    progress_bar.Fraction = 0;
                    progress_bar.Hide();
                    
                    if(timeout_id != 0) {
                        GLib.Source.Remove(timeout_id);
                        timeout_id = 0;
                    }
                    
                    return false;
                }
            
                progress_bar.Show();
                progress_bar.Pulse();
                return true;
            }
        }
        
        private void LoadModels()
        {
            ThreadPool.QueueUserWorkItem(LoadModelsAsync, null);
        }
        
        private void LoadModelsAsync(object data)
        {
            BeginWork();
            ModelFetcher fetcher = new ModelFetcher();
            
            try {
                fetcher.Download();
                Gtk.Application.Invoke(delegate {
                    model_store = new ModelTreeStore(fetcher.Models);
                    combo_box.Model = model_store;
                });
            } catch {
            }
            
            StopWork();
        }
        
        private void SubmitData()
        {
            IpodModel model = null;
            TreeIter iter;
            
            if(combo_box.GetActiveIter(out iter)) {
                model = (IpodModel)model_store.GetValue(iter, 1);
            }
            
            string data = "";
            
            if(model != null) {
                data += String.Format("{0}&", model);
            }
            
            data += String.Format("serial={0}&user_model={1}", 
                device.SerialNumber == null ? "" : device.SerialNumber,
                model_entry.Text);
            
            ThreadPool.QueueUserWorkItem(SubmitDataAsync, data);
            
            Sensitive = false;
        }

        private void WriteNoAskFile ()
        {
            if (dont_ask_button.Active && device.SerialNumber != null) {
                File.Open (String.Format ("{0}/.ipod-data-submit-{1}", Environment.GetEnvironmentVariable ("HOME"),
                                          device.SerialNumber), FileMode.Create).Close ();
            }
            
        }
        
        private void SubmitDataAsync(object data)
        {
            BeginWork();
            new DataSubmit((string)data);
            StopWork();
            WriteNoAskFile ();
            Application.Quit();
        }
    }

    public class ModelTreeStore : Gtk.TreeStore
    {
        private IpodModels models;
        
        public ModelTreeStore(IpodModels models) : base(typeof(string), typeof(IpodModel))
        {
            this.models = models;
            BuildTree();
        }
        
        private static string GenerationString(uint generation)
        {
            string suffix;
            
            switch(generation) {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
                default: suffix = "th"; break;
            } 
            
            return String.Format("{0}{1} Generation", generation, suffix);
        }
        
        private void BuildTree()
        {
            foreach(IpodModel model in models.Models) {
                if(model.Ignore) {
                    continue;
                }
            
                TreeIter name_parent;
                if(FindNodeByName(model.Name, TreeIter.Zero, out name_parent)) {
                    TreeIter capacity_parent;
                    string name_cap = String.Format("{0}, {1}", model.Name, model.CapacityString);
                    
                    if(FindNodeByName(name_cap, name_parent, out capacity_parent)) {
                        AppendValues(capacity_parent, String.Format("{0}, {1}, {2}", name_cap, 
                            model.Color == null ? "White" : model.Color, 
                            GenerationString(model.Generation)), model);
                    } else {
                        AppendValues(name_parent, name_cap, model);
                    }
                } else {
                    AppendValues(model.Name, model);
                }
            }
        }
        
        private bool FindNodeByName(string name, TreeIter parent, out TreeIter iter) 
        {
            TreeIter cmp_iter;
            
            for(int i = 0, n = TreeIter.Zero.Equals(parent) ? IterNChildren() : IterNChildren(parent); i < n; i++) {
                bool result = TreeIter.Zero.Equals(parent) ?
                    IterNthChild(out cmp_iter, i) :
                    IterNthChild(out cmp_iter, parent, i);
                
                if(result) {
                    string a = (string)GetValue(cmp_iter, 0);
                    if(a == name) {
                        iter = cmp_iter;
                        return true;
                    }
                }
            }
            
            iter = TreeIter.Zero;
            return false;
        }
    }

    public class ModelFetcher
    {
        private static readonly string models_url = "http://banshee-project.org/files/ipod.service/ipodmodels.xml";
        private IpodModels models;
        
        public ModelFetcher()
        {
        }

        public void Download()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(models_url);
            request.UserAgent = "iPod Data Submission Tool 1.0";
            request.KeepAlive = false;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using(Stream stream = response.GetResponseStream()) {
                XmlSerializer serializer = new XmlSerializer(typeof(IpodModels));
                models = (IpodModels)serializer.Deserialize(stream);
            }

            response.Close();
        }
        
        public IpodModels Models {
            get { return models; }
        }
    }
    
    public class DataSubmit
    {
        public DataSubmit(string postData)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://banshee-project.org/files/ipod.service/ipodsubmit.php");
            request.UserAgent = "iPod Data Submission Tool 1.0";
            request.KeepAlive = false;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            
            byte [] bytes = System.Text.Encoding.ASCII.GetBytes(postData);
            request.ContentLength = bytes.Length;
            
            Stream stream = request.GetRequestStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string response_string = reader.ReadToEnd();
            if(response_string == "OK") {
            }
        }
    }

    [XmlRoot(ElementName="ipod-models")]
    public class IpodModels
    {
        [XmlElement("model", typeof(IpodModel))]
        public ArrayList Models = new ArrayList();
    }

    public class IpodModel
    {
        [XmlElement(ElementName="number")]
        public string Number;
         
        [XmlElement(ElementName="capacity")]
        public uint Capacity;
        
        [XmlElement(ElementName="name")]
        public string Name;
         
        [XmlElement(ElementName="color")]
        public string Color;
         
        [XmlElement(ElementName="generation")]
        public uint Generation;
        
        [XmlElement(ElementName="ignore")]
        public bool Ignore;
        
        [XmlIgnore]
        public string CapacityString {
            get { return String.Format("{0} GB", Capacity / 1024); }
        }
         
        public override string ToString()
        {
            return String.Format("name={0}&color={1}&capacity={2}&generation={3}&number={4}",
                HttpUtility.UrlEncode(Name), 
                HttpUtility.UrlEncode(Color), 
                HttpUtility.UrlEncode(Capacity.ToString()), 
                HttpUtility.UrlEncode(Generation.ToString()),
                HttpUtility.UrlEncode(Number));
        }
    }
}
