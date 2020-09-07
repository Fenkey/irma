#.EXPORT_ALL_VARIABLES:

DEBUG = no
CC = gcc
MCS = mcs
MAKE = make
NETSDK = 4.5 #.Net framework sdk version

PREFIX = $(HOME)/local/irma
INSDIR = $(shell echo $(PREFIX) | sed 's/\/*$$//')
BINDIR = $(INSDIR)/bin
ASSEMBLYDIR = $(INSDIR)/assembly

export DEBUG CC MCS NETSDK BINDIR ASSEMBLYDIR

all: call kit

config:
	sh Configure

call:
	$(MAKE) -C irmacall

kit:
	$(MAKE) -C irmakit

install:
	mkdir -p $(INSDIR) $(BINDIR) $(ASSEMBLYDIR)
	$(MAKE) -C irmacall install
	$(MAKE) -C irmakit install

uninstall:
	rm -rf $(INSDIR)

clean-call:
	$(MAKE) -C irmacall clean

clean-kit:
	$(MAKE) -C irmakit clean

clean-config:
	rm config.h config.in

clean:
	$(MAKE) -C irmacall clean
	$(MAKE) -C irmakit clean
