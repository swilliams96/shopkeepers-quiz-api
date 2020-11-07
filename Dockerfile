FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["ShopkeepersQuiz.Api/ShopkeepersQuiz.Api.csproj", "ShopkeepersQuiz.Api/"]
RUN dotnet restore "ShopkeepersQuiz.Api/ShopkeepersQuiz.Api.csproj"
COPY . .
WORKDIR "/src/ShopkeepersQuiz.Api"
RUN dotnet build "ShopkeepersQuiz.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopkeepersQuiz.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopkeepersQuiz.Api.dll"]