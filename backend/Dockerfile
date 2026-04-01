# Build from repo root:
#   cd backend
#   docker build --platform=linux/amd64 -f Dockerfile.dev -t wm-backend .
#
# Run API container 
#   docker run --rm -p 5280:5280 wm-backend

# ---------- DEV STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install pandoc (docx/pdf) and Node.js/npm (mermaid-cli)
RUN apt-get update && apt-get install -y \
    pandoc \
    nodejs \
    npm \
    # Core Chrome / Puppeteer deps
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxshmfence1 \
    libdrm2 \
    libgbm1 \
    libcairo2 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libasound2 \
    libgtk-3-0 \
    ca-certificates \
    fonts-liberation \
 && rm -rf /var/lib/apt/lists/*
 
RUN dotnet tool install --global Microsoft.PowerApps.CLI.Tool --version 1.49.4
RUN npm install -g @mermaid-js/mermaid-cli

ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src

# Copy solution file
COPY backend.sln ./

# Copy project files (for layer caching)
COPY src/backend.Api/backend.csproj src/backend.Api/
COPY src/backend.Application/backend.Application.csproj src/backend.Application/
COPY src/backend.Domain/backend.Domain.csproj src/backend.Domain/
COPY src/backend.Infrastructure/backend.Infrastructure.csproj src/backend.Infrastructure/
COPY tests/unit/Domain.Tests/Domain.Tests.csproj tests/unit/Domain.Tests/
COPY tests/unit/Application.Tests/Application.Tests.csproj tests/unit/Application.Tests/
COPY tests/unit/Api.Tests/Api.Tests.csproj tests/unit/Api.Tests/
COPY tests/unit/Infrastructure.Tests/Infrastructure.Tests.csproj tests/unit/Infrastructure.Tests/
COPY tests/integration/Api.Tests/Api.Tests.csproj tests/integration/Api.Tests/
COPY tests/integration/Application.Tests/Application.Tests.csproj tests/integration/Application.Tests/


# Restore all projects : backend sln
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Move to API project folder
WORKDIR /src/src/backend.Api

# Expose dev port
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://0.0.0.0:5280
EXPOSE 5280

# Run in watch mode (like your local dotnet watch)
CMD ["dotnet", "watch", "--quiet", "run", "--urls", "http://0.0.0.0:5280"]