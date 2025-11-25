# Use official Node.js LTS image
FROM node:20-alpine

# Set working directory in container
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm install --production

# Copy the rest of the app
COPY . .

# Expose a port if needed (optional)
# EXPOSE 3000

# Run the notifier.js file
CMD ["node", "notifier.js"]