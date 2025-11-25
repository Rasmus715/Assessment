# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Accept build arguments for environment variables
ARG NEXT_PUBLIC_GATEWAY_HTTP_URL
ARG NEXT_PUBLIC_GATEWAY_HTTPS_URL
ARG NEXT_PUBLIC_SIGNALR_URL
ARG NEXT_PUBLIC_GRAPHQL_URL
ARG NEXT_PUBLIC_REST_API_URL

# Set environment variables for build time
ENV NEXT_PUBLIC_GATEWAY_HTTP_URL=$NEXT_PUBLIC_GATEWAY_HTTP_URL
ENV NEXT_PUBLIC_GATEWAY_HTTPS_URL=$NEXT_PUBLIC_GATEWAY_HTTPS_URL
ENV NEXT_PUBLIC_SIGNALR_URL=$NEXT_PUBLIC_SIGNALR_URL
ENV NEXT_PUBLIC_GRAPHQL_URL=$NEXT_PUBLIC_GRAPHQL_URL
ENV NEXT_PUBLIC_REST_API_URL=$NEXT_PUBLIC_REST_API_URL

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the Next.js app
RUN npm run build

# Production stage
FROM node:20-alpine AS runner

WORKDIR /app

ENV NODE_ENV=production

# Copy package files
COPY package*.json ./

# Install only production dependencies
RUN npm ci --only=production && npm cache clean --force

# Copy built application from builder
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/next.config.ts ./next.config.ts

# Expose port
EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Start the application
CMD ["npm", "start"]

