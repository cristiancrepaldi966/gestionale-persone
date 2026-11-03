FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Prima solo il file di progetto: le librerie scaricate restano nella cache di Docker
# e si riscaricano solo quando cambia il .csproj, non a ogni modifica del codice.
COPY GestionalePersone.csproj .

RUN dotnet restore "GestionalePersone.csproj"

COPY . .

RUN dotnet publish "GestionalePersone.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# OpenSCAD genera i file STL del modulo Stampa 3D; i font DejaVu servono per i testi.
RUN apt-get update \
    && apt-get install -y --no-install-recommends openscad fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "GestionalePersone.dll"]