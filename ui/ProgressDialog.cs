using System;
using Gtk;
using GLib;

namespace IPod {

    public class ProgressDialog : Dialog {

        private SongDatabase db;
        private Label label;
        private ProgressBar bar;
        private ThreadNotify notify;

        private bool visible;
        private double fraction;
        private string message;
        
        public SongDatabase SongDatabase {
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
            hbox.PackStart (new Image (Stock.DialogInfo, IconSize.Dialog), false, false, 0);

            label = new Label ("");
            label.Xalign = 0.0f;
            label.UseMarkup = true;
            label.Ellipsize = Pango.EllipsizeMode.Middle;
            hbox.PackStart (label, true, true, 0);

            vbox.PackStart (hbox, true, false, 0);

            bar = new ProgressBar ();
            vbox.PackStart (bar, true, false, 0);

            VBox.PackStart (vbox, false, false, 0);
            VBox.ShowAll ();

            notify = new ThreadNotify (new ReadyEvent (OnNotify));
        }

        private void OnSaveStarted (object o, EventArgs args) {
            visible = true;
            message = "<b>Preparing...</b>";
            notify.WakeupMain ();
        }

        private void OnSaveProgressChanged (SongDatabase db, Song song,
                                            int current, int total) {
            lock (this) {
                message = String.Format ("<b>Adding '{0}' ({1} of {2})</b>",
                                         GLib.Markup.EscapeText (song.Title),
                                         current, total);
                fraction = (double) current / (double) total;
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

