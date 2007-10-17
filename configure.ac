AC_INIT([ipod-sharp], [0.8.0])
AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar])
AM_MAINTAINER_MODE

DISTCHECK_CONFIGURE_FLAGS="--disable-docs"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

ASM_VERSION="2.0.0.0"
AC_SUBST(ASM_VERSION)

AC_PROG_INSTALL

SHAMROCK_CHECK_MONO_MODULE(1.1.10)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONODOC
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
	System
	System.Web
])

SHAMROCK_CHECK_MONO_NUNIT

PKG_CHECK_MODULES(PODSLEUTH, podsleuth >= 0.6.0)
PKG_CHECK_MODULES(NDESK_DBUS, ndesk-dbus-1.0 ndesk-dbus-glib-1.0)

SHAMROCK_EXPAND_LIBDIR

AC_OUTPUT([
Makefile
ipod-sharp.pc
ipod-sharp-ui.pc
src/Makefile
src/Firmware/Makefile
ui/Makefile
docs/Makefile
])

# tests/Makefile
# tools/Makefile

echo "
    Installation Prefix:     ${prefix}
    C# Compiler:             ${MCS}

    Developer Documentation: ${enable_docs}
"

