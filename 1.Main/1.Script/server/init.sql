-- Set character set
SET NAMES utf8mb4;
ALTER DATABASE IF EXISTS yjp CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Create universities table
CREATE TABLE IF NOT EXISTS universities (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL UNIQUE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL UNIQUE,
  email VARCHAR(100) NOT NULL UNIQUE,
  password VARCHAR(255) NOT NULL,
  user_type ENUM('university', 'regular', 'admin') NOT NULL,
  university_id INT NULL,
  student_id VARCHAR(20) NULL,
  grade ENUM('1', '2', '3', '4') NULL,
  is_approved BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (university_id) REFERENCES universities(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert default universities
INSERT INTO universities (name) VALUES 
('영진전문대'),
('구미대학교');

-- Insert admin user (password: admin123)
INSERT INTO users (username, email, password, user_type, is_approved)
VALUES (
  'admin',
  'admin@metaplay.kr',
  '$2b$10$8WjZJXZJXZJXZJXZJXZJX.ZJXZJXZJXZJXZJXZJXZJXZJXZJXZJXZJ',
  'admin',
  TRUE
); 