FROM node:22-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Expose Angular dev server port
EXPOSE 4201

# Start dev server with hot reload for containers
CMD ["npm", "run", "start:docker"]