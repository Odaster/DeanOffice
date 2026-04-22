FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DeanOfficeCourseWork.csproj ./
RUN dotnet restore DeanOfficeCourseWork.csproj

COPY . ./
RUN dotnet publish DeanOfficeCourseWork.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

RUN mkdir -p /app/Backups /app/wwwroot/uploads/profiles

EXPOSE 8080
ENTRYPOINT ["dotnet", "DeanOfficeCourseWork.dll"]
