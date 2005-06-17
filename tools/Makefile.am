
MCS_FLAGS = -g

TOOLS = ipod-tool.exe

IPOD_TOOL_SOURCES = $(srcdir)/PodTool.cs

all: $(TOOLS)

ipod-tool.exe: $(IPOD_TOOL_SOURCES)
	$(MCS) $(MCS_FLAGS) -r:$(top_builddir)/src/ipod-sharp.dll -target:exe -o $@ $(IPOD_TOOL_SOURCES)

EXTRA_DIST = $(IPOD_TOOL_SOURCES)
CLEANFILES = $(TOOLS)
