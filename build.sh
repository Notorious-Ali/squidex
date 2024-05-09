#!/bin/sh

SQUIDEX_TAG=7.8.2
SQUIDEX_FRONTEND_TAG=18.10

# Build and tag multi-platform
docker buildx build --platform linux/amd64,linux/arm64 --build-arg "SQUIDEX_TAG=$SQUIDEX_TAG" --build-arg "SQUIDEX_FRONTEND_TAG=$SQUIDEX_FRONTEND_TAG" -t squid/squidex:${SQUIDEX_TAG} .

# Build and tag amd64 only. This will be quick, since all amd64 layers has already been built locally.
docker buildx build --load --platform linux/amd64 --build-arg "SQUIDEX_TAG=$SQUIDEX_TAG" --build-arg "SQUIDEX_FRONTEND_TAG=$SQUIDEX_FRONTEND_TAG" -t squid/squidex:$SQUIDEX_TAG-amd64 .

# Build and tag arm64 only. This will be quick, since all arm64 layers has already been built locally.
docker buildx build --load --platform linux/arm64 --build-arg "SQUIDEX_TAG=$SQUIDEX_TAG" --build-arg "SQUIDEX_FRONTEND_TAG=$SQUIDEX_FRONTEND_TAG" -t squid/squidex:$SQUIDEX_TAG-arm64 .
