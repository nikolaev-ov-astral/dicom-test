FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-focal

WORKDIR /app

COPY . .

RUN ls

ENTRYPOINT [ "dotnet", "DicomTest.dll" ]