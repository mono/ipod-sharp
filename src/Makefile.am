SUBDIRS = Firmware

MCS_FLAGS = -debug

ASSEMBLY = ipod-sharp.dll

ipoddir = $(prefix)/lib/ipod-sharp
ipod_DATA = $(ASSEMBLY)

ASSEMBLY_SOURCES =			\
	$(srcdir)/Album.cs	\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/ArtworkFormat.cs \
	$(srcdir)/DatabaseReadException.cs	\
	$(srcdir)/DatabaseWriteException.cs	\
	$(srcdir)/Device.cs \
	$(srcdir)/DeviceBusyException.cs	\
	$(srcdir)/DeviceException.cs	\
	$(srcdir)/DeviceManager.cs	\
	$(srcdir)/DeviceModel.cs	\
	$(srcdir)/DeviceGeneration.cs	\
	$(srcdir)/EndianBinaryWriter.cs		\
	$(srcdir)/Equalizer.cs		\
	$(srcdir)/ObjectDumper.cs		\
	$(srcdir)/Photo.cs	\
	$(srcdir)/PhotoDatabase.cs		\
	$(srcdir)/Playlist.cs		\
	$(srcdir)/ShuffleTrackDatabase.cs \
	$(srcdir)/SportKitManager.cs	\
	$(srcdir)/Thumbnail.cs		\
	$(srcdir)/Track.cs		\
	$(srcdir)/TrackDatabase.cs	\
	$(srcdir)/Utility.cs	\
	$(srcdir)/Hash58.cs \
	$(srcdir)/ProductionInfo.cs \
	$(srcdir)/VolumeInfo.cs \
	$(srcdir)/ModelInfo.cs \
	$(srcdir)/Hal/HalDevice.cs	\
	$(srcdir)/Hal/HalDeviceManager.cs	\
	$(srcdir)/Hal/Volume.cs	\
	$(srcdir)/Hal/Device.cs	\
	$(srcdir)/Hal/Manager.cs


ASSEMBLY_RESOURCES =

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(ASSEMBLY_RESOURCES)
	$(MCS) $(MCS_FLAGS) -unsafe -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -out:$@ $(ASSEMBLY_SOURCES) $(ASSEMBLY_RESOURCES:%=-resource:%) -r:Mono.Posix -pkg:gtk-sharp-2.0 -r:ICSharpCode.SharpZipLib $(NDESK_DBUS_LIBS)

#install-data-hook:
#	$(GACUTIL) /i $(ASSEMBLY) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

EXTRA_DIST =			\
	$(ASSEMBLY_SOURCES)	\
	$(ASSEMBLY_RESOURCES)	\
	ipod-sharp.dll.config

CLEANFILES = $(ASSEMBLY) *.mdb
MAINTAINERCLEANFILES = Makefile.in

