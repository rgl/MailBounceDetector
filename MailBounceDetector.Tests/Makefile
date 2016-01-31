ifeq ($(OS), Windows_NT)
# NB this is normally at C:\Program Files (x86)\MSBuild\14.0\bin
XBUILD=MSBuild.exe
else
XBUILD=xbuild
endif
BIN=bin/Release/MailBounceDetector.Tests.dll

all: $(BIN)

$(BIN): *.cs
	nuget restore -SolutionDirectory ..
	$(XBUILD) -p:Configuration=Release

clean:
	rm -rf bin obj

.PHONY: all clean
