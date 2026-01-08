FROM node:12-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Expose Angular dev server port
EXPOSE 4200

# Start dev server with hot reload for containers
CMD ["npm", "run", "start:container"]