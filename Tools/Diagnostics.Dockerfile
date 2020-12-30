FROM mcr.microsoft.com/dotnet/sdk:5.0 as sdk

RUN mkdir /root/.dotnet/tools
ENV PATH="/root/.dotnet/tools:${PATH}"

# In build stage
# Install desired .NET CLI diagnostics tools
RUN dotnet tool install --global dotnet-trace
RUN dotnet tool install --global dotnet-counters
RUN dotnet tool install --global dotnet-dump

# RUN dotnet tool install --global dotnet-ef

WORKDIR /diagnostics
ENTRYPOINT [ "/bin/bash" ]