using System;
using System.Reflection;
using Gtk;
using GLib;

namespace IPod {

    public class ProgressDialog : Dialog {

        private TrackDatabase db;
        private Label label;
        private ProgressBar bar;
        private ThreadNotify notify;

        private bool visible;
        private double fraction;
        private string message;
        
        public TrackDatabase TrackDatabase {
            get { return db; }
            set {
                if (db != null) {
                    // disconnect
                    db.SaveStarted -= OnSaveStarted;
                    db.SaveProgressChanged -= OnSaveProgressChanged;
                    db.SaveEnded -= OnSaveEnded;
                }
                
                db = value;

                if (db != null) {
                    // connect
                    db.SaveStarted += OnSaveStarted;
                    db.SaveProgressChanged += OnSaveProgressChanged;
                    db.SaveEnded += OnSaveEnded;
                }
            }
        }
        
        public ProgressDialog (Window parent) : base () {
            this.Title = "Updating iPod...";
            this.HasSeparator = false;
            this.TransientFor = parent;
            this.DefaultWidth = 400;
            
            VBox vbox = new VBox (false, 6);
            vbox.BorderWidth = 6;

            HBox hbox = new HBox (false, 6);

            Gdk.PixbufAnimation animation = new Gdk.PixbufAnimation (Assembly.GetExecutingAssembly (),
                                                                     "ipod.gif");
            hbox.PackStart (new Gtk.Image (animation), false, false, 0);

            label = new Label ("");
            label.Xalign = 0.0f;
            label.UseMarkup = true;

            SetEllipsize (label);
            hbox.PackStart (label, true, true, 0);

            vbox.PackStart (hbox, true, false, 0);

            bar = new ProgressBar ();
            vbox.PackStart (bar, true, false, 0);

            VBox.PackStart (vbox, false, false, 0);
            VBox.ShowAll ();

            notify = new ThreadNotify (new ReadyEvent (OnNotify));
        }

        private void SetEllipsize (Label label) {
            MethodInfo method = typeof (GLib.Object).GetMethod ("SetProperty",
                                                                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke (label, new object[] { "ellipsize", new GLib.Value (3) });
        }

        private void OnSaveStarted (object o, EventArgs args) {
            visible = true;
            message = "<b>Preparing...</b>";
            notify.WakeupMain ();
        }

        private void OnSaveProgressChanged (object o, TrackSaveProgressArgs args) {
            lock (this) {
                if (args.CurrentTrack != null) {
                    string padstr = String.Format ("Adding {0} of {0}", args.TracksTotal);
                    
                    message = String.Format ("Adding {0} of {1}", args.TracksCompleted + 1, args.TracksTotal);
                    message = message.PadLeft (padstr.Length);
                    
                    message = String.Format ("<b>{0}: {1}</b>", message, GLib.Markup.EscapeText (args.CurrentTrack.Title));
                } else {
                    message = String.Format ("<b>Finishing...</b>");
                }

                fraction = args.TotalProgress;
                
                notify.WakeupMain ();
            }
        }

        private void OnSaveEnded (object o, EventArgs args) {
            visible = false;
            notify.WakeupMain ();
        }

        private void OnNotify () {
            lock (this) {
                if (this.visible && !Visible)
                    Show ();
                else if (!this.visible && Visible)
                    Hide ();
                
                label.Markup = message;
                bar.Fraction = fraction;
            }
        }
    }
}

