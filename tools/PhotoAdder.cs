using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using IPod;
using Gtk;

public class EntryPoint {

    private static string directory;

    private static void AddThumbnails (Device device, Photo photo, Gdk.Pixbuf pixbuf) {
        foreach (ArtworkFormat format in device.LookupArtworkFormats (ArtworkUsage.Photo)) {
            byte[] bytes;
            short padX, padY;

            bytes = ArtworkHelpers.ToBytes (format, pixbuf, out padX, out padY);

            Thumbnail thumbnail = photo.CreateThumbnail ();
            thumbnail.Format = format;
            thumbnail.Width = (short) pixbuf.Width;
            thumbnail.Height = (short) pixbuf.Height;
            thumbnail.HorizontalPadding = padX;
            thumbnail.VerticalPadding = padY;

            thumbnail.SetData (bytes);
        }
    }

    private static void AddDirectory (Device device, Album album, string dir) {
        foreach (string file in Directory.GetFiles (dir)) {
            try {
                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (file);
                
                Photo photo = device.PhotoDatabase.CreatePhoto ();

                AddThumbnails (device, photo, pixbuf);
                pixbuf.Dispose ();

                photo.FullSizeFileName = file;
                album.Add (photo);
            } catch (GLib.GException e) {
            } catch (Exception e) {
                Console.WriteLine (e);
            }
        }

        foreach (string child in Directory.GetDirectories (dir)) {
            AddDirectory (device, album, child);
        }
    }

    private static void DoStuff () {
        foreach (Device device in Device.ListDevices ()) {
            Console.WriteLine ("Adding photos to '{0}'", device.Name);
            
            Album album = device.PhotoDatabase.CreateAlbum (Path.GetFileName (directory));

            Console.WriteLine ("Finding images and creating thumbnails...");
            AddDirectory (device, album, directory);

            device.PhotoDatabase.SaveProgressChanged += delegate (object o, PhotoSaveProgressArgs args) {
                Console.WriteLine ("Save Progress: " + (int) (args.Percent * 100));
            };

            device.PhotoDatabase.Save ();
            Console.WriteLine ("Finished.");
        }

        Application.Quit ();
    }

    public static void Main (string[] args) {

        if (args.Length == 0) {
            Console.Error.WriteLine ("Usage: photo-adder <dir>");
            Console.Error.WriteLine ("Adds all photos in directory <dir> to all connected devices");
            return;
        }

        Application.Init ();

        directory = args[0];
        Thread thread = new Thread (DoStuff);
        thread.Start ();
        Application.Run ();
    }
}
