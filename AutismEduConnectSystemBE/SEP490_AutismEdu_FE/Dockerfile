FROM node:18 AS build

WORKDIR /app

# Copy package.json and package-lock.json to install dependencies
COPY package*.json ./

# Install dependencies
RUN npm i

# Copy the rest of the React app source code
COPY . .
EXPOSE 5173

CMD ["npm", "run", "dev"]