all:
	nuget restore
	xbuild
	nuget pack