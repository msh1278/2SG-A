module.exports = {
  apps: [{
    name: "server",
    script: "./server.js",
    watch: true,
    ignore_watch: ["node_modules", "log.xlsx", "*.log", "logs"],
    env: {
      "NODE_ENV": "production",
      "DB_HOST": "localhost",
      "DB_USER": "root",
      "DB_PASSWORD": "Emfprhsqhf1!",
      "DB_NAME": "yjp",
      "JWT_SECRET": "your-secret-key"
    }
  }]
} 