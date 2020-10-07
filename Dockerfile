FROM mcr.microsoft.com/dotnet/sdk:5.0

COPY . /source
WORKDIR /source
RUN dotnet build -c Release -o /build
EXPOSE 5000

RUN mkdir /data
WORKDIR /data
