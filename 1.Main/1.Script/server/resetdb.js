import mysql from 'mysql2/promise';

const main = async () => {
  let connection;
  try {
    // 데이터베이스 접속 정보
    const dbConfig = {
      host: process.env.DB_HOST || 'localhost',
      user: process.env.DB_USER || 'root',
      password: process.env.DB_PASSWORD || 'Emfprhsqhf1!',
      charset: 'utf8mb4',
      multipleStatements: true
    };

    console.log('MySQL에 연결 중...');
    connection = await mysql.createConnection(dbConfig);
    
    console.log('기존 yjp 데이터베이스 삭제 중...');
    await connection.query('DROP DATABASE IF EXISTS yjp');
    
    console.log('새로운 yjp 데이터베이스 생성 중...');
    await connection.query('CREATE DATABASE yjp CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
    
    console.log('yjp 데이터베이스로 전환 중...');
    await connection.query('USE yjp');
    
    console.log('universities 테이블 생성 중...');
    await connection.query(`
      CREATE TABLE IF NOT EXISTS universities (
        id INT AUTO_INCREMENT PRIMARY KEY,
        name VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `);
    
    console.log('users 테이블 생성 중...');
    await connection.query(`
      CREATE TABLE IF NOT EXISTS users (
        id INT AUTO_INCREMENT PRIMARY KEY,
        username VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE,
        email VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE,
        password VARCHAR(255) NOT NULL,
        user_type ENUM('university', 'regular', 'admin') NOT NULL,
        university_id INT NULL,
        student_id VARCHAR(20) NULL,
        grade ENUM('1', '2', '3', '4') NULL,
        is_approved BOOLEAN DEFAULT FALSE,
        is_online BOOLEAN DEFAULT FALSE,
        last_activity TIMESTAMP NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (university_id) REFERENCES universities(id)
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `);
    
    console.log('기본 대학교 데이터 삽입 중...');
    await connection.query("INSERT INTO universities (name) VALUES ('영진전문대'), ('구미대학교')");
    
    console.log('관리자 계정 생성 중...');
    await connection.query(`
      INSERT INTO users (username, email, password, user_type, is_approved)
      VALUES (
        'admin',
        'admin@metaplay.kr',
        '$2b$10$8WjZJXZJXZJXZJXZJXZJX.ZJXZJXZJXZJXZJXZJXZJXZJXZJXZJXZJ',
        'admin',
        TRUE
      )
    `);
    
    console.log('데이터베이스 초기화 완료!');
    
  } catch (error) {
    console.error('데이터베이스 초기화 오류:', error);
  } finally {
    if (connection) {
      await connection.end();
    }
    process.exit(0);
  }
};

main(); 