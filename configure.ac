AC_INIT(README)
AC_CANONICAL_SYSTEM

MAJOR_VERSION=0
MINOR_VERSION=5
MICRO_VERSION=14
VERSION=$MAJOR_VERSION.$MINOR_VERSION.$MICRO_VERSION
                                                                                
AM_INIT_AUTOMAKE(ipod-sharp, $VERSION)
                                                                                
AC_SUBST(MAJOR_VERSION)
AC_SUBST(MINOR_VERSION)
AC_SUBST(MICRO_VERSION)
                                                                                
AC_DEFINE_UNQUOTED(MAJOR_VERSION, $MAJOR_VERSION, [Major Version Number])
AC_DEFINE_UNQUOTED(MINOR_VERSION, $MINOR_VERSION, [Minor Version Number])
AC_DEFINE_UNQUOTED(MICRO_VERSION, $MICRO_VERSION, [Micro Version Number])

AM_MAINTAINER_MODE

PKG_CHECK_MODULES(IPODDEVICE, ipoddevice >= 0.3.5 gtk-sharp-2.0)

dnl Mono	   
AC_PATH_PROG(MONO, mono)
if test "x$MONO" = "x" ; then
   AC_MSG_ERROR([Can not find "mono" in your PATH])
fi

dnl mcs	   
AC_PATH_PROG(MCS, mcs)
if test "x$MCS" = "x" ; then
   AC_MSG_ERROR([Can not find "mcs" in your PATH])
fi

AC_ARG_ENABLE(docs, [  --disable-docs    Do not build documentation],
  with_docs=no, with_docs=yes)

if test "x$with_docs" = "xyes"; then
        AC_PATH_PROG(MONODOCER, monodocer, no)
        if test "x$MONODOC" = "xno"; then
                AC_MSG_ERROR([You need to install monodoc])
        fi

        AC_PATH_PROG(MDASSEMBLER, mdassembler, no)
        if test "x$MDASSEMBLER" = "xno"; then
                AC_MSG_ERROR([You need to install mdassembler])
        fi
        AM_CONDITIONAL(BUILD_DOCS, true)
else
        AC_MSG_NOTICE([not building API documentation])
        AM_CONDITIONAL(BUILD_DOCS, false)
fi

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
        AC_MSG_ERROR([You need to install gacutil])
fi

AC_ARG_ENABLE(tests, [  --disable-tests   Do not build nunit tests],
  with_nunit=no, with_nunit=yes)
                                                                                                    
if test "x$with_nunit" = "xyes"; then
  AM_CONDITIONAL(BUILD_TESTS, true)
else
  AM_CONDITIONAL(BUILD_TESTS, false)
fi


AC_OUTPUT([
Makefile
ipod-sharp.pc
ipod-sharp-ui.pc
src/Makefile
ui/Makefile
tools/Makefile
tests/Makefile
])
