ifeq ($(OS), Windows_NT)
# NB this is normally at C:\Program Files (x86)\MSBuild\14.0\bin
XBUILD=MSBuild.exe
else
XBUILD=xbuild
endif
BIN=bin/Release/MailBounceDetector.dll
PKG_VERSION=$(shell grep '^\[assembly: AssemblyVersion' Properties/AssemblyInfo.cs | sed -E 's,.+"(.+)".+,\1,g')
PKG=MailBounceDetector.$(PKG_VERSION).nupkg

all: pack

pack: $(PKG)

$(PKG): $(BIN) MailBounceDetector.nuspec
	nuget pack MailBounceDetector.csproj -Prop Configuration=Release -Verbosity detailed

pack-publish: $(PKG)
	@#nuget setApiKey Your-API-Key
	nuget push $(PKG)

$(BIN): *.cs
	nuget restore -SolutionDirectory ..
	$(XBUILD) //p:Configuration=Release

clean:
	rm -rf bin obj $(PKG)

.PHONY: all pack pack-publish clean
