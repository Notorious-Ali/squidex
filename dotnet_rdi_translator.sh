#!/bin/sh

# Takes the provided first argument and translates docker platform to dotnet runtime identifier (rid)
case "$1" in
	"linux/amd64") DOTNET_RUNTIME="linux-musl-x64" ;;
	"linux/arm64") DOTNET_RUNTIME="linux-musl-arm64" ;;
	*) DOTNET_RUNTIME="$1"
esac

export DOTNET_RUNTIME
