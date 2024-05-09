ARG SQUIDEX_TAG=
ARG SQUIDEX_FRONTEND_TAG=

## Backend build env
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 as backend-build
ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG SQUIDEX_TAG
SHELL ["/bin/bash", "-c"]
WORKDIR /

COPY ./dotnet_rdi_translator.sh ./dotnet_rdi_translator.sh

RUN git clone --depth 1 --branch $SQUIDEX_TAG https://github.com/Squidex/squidex.git

RUN source /dotnet_rdi_translator.sh ${TARGETPLATFORM} && \
    dotnet publish /squidex/backend/src/Squidex/Squidex.csproj \
        -c Release \
        -r $DOTNET_RUNTIME \
        --self-contained \
        -p:PublishSingleFile=true \
        -p:version=$SQUIDEX_TAG \
        -o /publish/


## Build Frontend
# NOTE: Use TARGETPLATFORM to ensure npm install is done for expected TARGET (e.g. linux/arm64).
# TODO: Is this needed, or can it be done any other way?
FROM --platform=$TARGETPLATFORM squidex/frontend-build:$SQUIDEX_FRONTEND_TAG as frontend-build
ARG TARGETPLATFORM
ARG SQUIDEX_FRONTEND_TAG
WORKDIR /
ENV CONTINUOUS_INTEGRATION=1

# Copy frontend from backend-env stage, so we don't have to install git and clone repository again.
# TODO: Any more efficient way of doing this?
COPY --from=backend-build /squidex/frontend/package*.json /squidex/frontend/

RUN cd /squidex/frontend/ && \
    npm install --loglevel=error --force

COPY --from=backend-build /squidex/frontend/ /squidex/frontend/

RUN cd /squidex/frontend/ && \
    npm run test:coverage && \
    npm run build

RUN cp -a /squidex/frontend/build /build/

## Runtime env
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine as runtime
ARG TARGETPLATFORM
ARG SQUIDEX_TAG
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apk add icu

# Copy from build stages
COPY --from=backend-build /publish/ /app/
COPY --from=frontend-build /build/ /app/wwwroot/build/

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["/app/Squidex"]

ENV EXPOSEDCONFIGURATION__VERSION=$SQUIDEX_TAG
