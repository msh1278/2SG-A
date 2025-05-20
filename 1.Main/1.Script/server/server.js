const express = require('express');
const mysql = require('mysql2');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const cors = require('cors');
const { WebSocketServer } = require('ws');
const { RtcTokenBuilder, Role } = require('./nodejs/src/RtcTokenBuilder2.js');
const addLogToExcel = require('./logUse.js');

const app = express();
app.use(express.json());
app.use(cors());

// WebSocket Server
const wss = new WebSocketServer({ port: 9000 });

wss.on("connection", ws => {
    addLogToExcel("Unity connected!");

    // Set a ping interval to keep the connection alive
    const pingInterval = setInterval(() => {
        if (ws.readyState === 1) { // 1 = OPEN in WebSocket standard
            ws.ping();
        }
    }, 30000);

    ws.on("message", message => {
        if (Buffer.isBuffer(message)) {
            message = message.toString();
        }

        console.log("Raw WebSocket message received:", message);

        let splitMsg = message.split("{****}");
        console.log("Split message parts:", splitMsg.length, "parts");
        
        if (splitMsg.length < 2) {
            console.error("Malformed message, missing {****} separator:", message);
            ws.send(JSON.stringify({ error: "Malformed message format" }));
            return;
        }
        
        switch(splitMsg[0]) {
            case "Login":
                console.log("Login message data part:", splitMsg[1]);
                handleUnityLogin(ws, splitMsg[1]);
                break;
            case "Token":
                handleTokenGeneration(ws, splitMsg[1]);
                break;
            case "CustomChange":
                handleCustomChange(ws, splitMsg[1]);
                break;
            case "CustomGet":
                handleCustomGet(ws, splitMsg[1]);
                break;
            case "PosGet":
                handlePosGet(ws, splitMsg[1]);
                break;
            case "PosSet":
                handlePosSet(ws, splitMsg[1]);
                break;
            case "InventoryGet":
                handleInventoryGet(ws, splitMsg[1]);
                break;
            case "InventorySet":
                handleInventorySet(ws, splitMsg[1]);
                break;
            default:
                console.error("Unknown message type:", splitMsg[0]);
                ws.send(JSON.stringify({ error: "Unknown message type" }));
        }
    });

    ws.on("close", () => {
        clearInterval(pingInterval);
        addLogToExcel("Unity disconnected!");
    });

    ws.on("error", (error) => {
        console.error("WebSocket error:", error);
        clearInterval(pingInterval);
    });
});

// MySQL Connection
const db = mysql.createConnection({
  host: process.env.DB_HOST || 'localhost',
  user: process.env.DB_USER || 'root',
  password: process.env.DB_PASSWORD || 'Emfprhsqhf1!',
  database: process.env.DB_NAME || 'yjp',
  charset: 'utf8mb4'
});

// Agora Configuration
const appID = "f86b34d0a03c48979cd2535c603157ce";
const appCertificate = "2728f4458b83423592dbe431a6741680";

// Wait for MySQL to be ready
const connectWithRetry = () => {
  db.connect((err) => {
    if (err) {
      console.error('Error connecting to MySQL:', err);
      console.log('Retrying in 5 seconds...');
      setTimeout(connectWithRetry, 5000);
    } else {
      console.log('Connected to MySQL database');
      
      // Check database character set
      db.query('SHOW VARIABLES LIKE "character_set_%"', (err, results) => {
        if (err) {
          console.error('Error checking character set:', err);
        } else {
          console.log('MySQL Character Set Settings:');
          results.forEach(row => {
            console.log(`${row.Variable_name}: ${row.Value}`);
          });
        }
      });
      
      // Initialize database
      const initDb = async () => {
        try {
          // Set session character set to utf8mb4
          await db.promise().query('SET NAMES utf8mb4');
          
          // Check if the database is using utf8mb4
          const [charsetResult] = await db.promise().query('SELECT @@character_set_database, @@collation_database');
          console.log('Current Database Charset:', charsetResult[0]['@@character_set_database'], 'Collation:', charsetResult[0]['@@collation_database']);
          
          if (charsetResult[0]['@@character_set_database'] !== 'utf8mb4') {
            console.log('Warning: Database character set is not utf8mb4. Korean characters may not display correctly.');
            console.log('Attempting to alter database character set...');
            await db.promise().query('ALTER DATABASE yjp CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
            console.log('Database character set updated.');
          }

          // Create universities table if not exists
          await db.promise().query(`
            CREATE TABLE IF NOT EXISTS universities (
              id INT AUTO_INCREMENT PRIMARY KEY,
              name VARCHAR(100) NOT NULL UNIQUE,
              created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
          `);

          // Create users table if not exists with modified structure
          await db.promise().query(`
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
              is_online BOOLEAN DEFAULT FALSE,
              last_activity TIMESTAMP NULL,
              pos_x FLOAT DEFAULT 0,
              pos_y FLOAT DEFAULT 0,
              pos_z FLOAT DEFAULT 0,
              map_name VARCHAR(100) DEFAULT '',
              custom_data JSON NULL,
              inventory_data JSON NULL,
              created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
              FOREIGN KEY (university_id) REFERENCES universities(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
          `);

          // Add missing columns if they don't exist
          try {
            await db.promise().query(`
              ALTER TABLE users 
              ADD COLUMN IF NOT EXISTS pos_x FLOAT DEFAULT 0,
              ADD COLUMN IF NOT EXISTS pos_y FLOAT DEFAULT 0,
              ADD COLUMN IF NOT EXISTS pos_z FLOAT DEFAULT 0,
              ADD COLUMN IF NOT EXISTS map_name VARCHAR(100) DEFAULT '',
              ADD COLUMN IF NOT EXISTS custom_data JSON NULL,
              ADD COLUMN IF NOT EXISTS inventory_data JSON NULL
            `);
          } catch (error) {
            console.log('Columns already exist or error adding columns:', error);
          }

          // Check if universities exist, if not insert default ones
          const [universities] = await db.promise().query('SELECT * FROM universities');
          if (universities.length === 0) {
            await db.promise().query(`
              INSERT INTO universities (name) VALUES 
              ('영진전문대'),
              ('구미대학교')
            `);
            console.log('Default universities created');
          }

          // Check if admin user exists
          const [adminUsers] = await db.promise().query(
            'SELECT * FROM users WHERE user_type = ?',
            ['admin']
          );

          if (adminUsers.length === 0) {
            // Create admin user
            const hashedPassword = await bcrypt.hash('admin123', 10);
            await db.promise().query(
              'INSERT INTO users (username, email, password, user_type, is_approved) VALUES (?, ?, ?, ?, ?)',
              ['admin', 'admin@metaplay.com', hashedPassword, 'admin', true]
            );
            console.log('Admin user created');
          } else {
            console.log('Admin user already exists');
          }

          // Check if test user exists
          const [testUsers] = await db.promise().query(
            'SELECT * FROM users WHERE email = ?',
            ['test@test.com']
          );

          if (testUsers.length === 0) {
            // Create test user
            const hashedPassword = await bcrypt.hash('test123', 10);
            await db.promise().query(
              'INSERT INTO users (username, email, password, user_type, is_approved) VALUES (?, ?, ?, ?, ?)',
              ['testuser', 'test@test.com', hashedPassword, 'regular', true]
            );
            console.log('Test user created');
          } else {
            console.log('Test user already exists');
          }
          
          // List all users
          const [allUsers] = await db.promise().query('SELECT id, username, email, user_type, is_approved FROM users');
          // console.log('현재 사용자 목록:', allUsers);
          
        } catch (error) {
          console.error('Error initializing database:', error);
        }
      };

      initDb();
    }
  });
};

connectWithRetry();

// JWT Secret Key
const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key';

// Middleware to verify JWT token
const authenticateToken = (req, res, next) => {
  const token = req.headers['authorization']?.split(' ')[1];
  if (!token) return res.sendStatus(401);

  jwt.verify(token, JWT_SECRET, (err, user) => {
    if (err) return res.sendStatus(403);
    req.user = user;
    next();
  });
};

// Get universities list
app.get('/api/universities', async (req, res) => {
  try {
    const [universities] = await db.promise().query('SELECT * FROM universities');
    res.json(universities);
  } catch (error) {
    console.error('Error fetching universities:', error);
    res.status(500).json({ message: 'Failed to fetch universities' });
  }
});

// Register endpoint
app.post('/api/register', async (req, res) => {
  const { username, email, password, userType, universityId, studentId, grade } = req.body;
  
  try {
    // Check if user already exists
    const [existingUser] = await db.promise().query(
      'SELECT * FROM users WHERE username = ? OR email = ?',
      [username, email]
    );

    if (existingUser.length > 0) {
      return res.status(400).json({ message: 'User already exists' });
    }

    // Validate university student data
    if (userType === 'university') {
      if (!universityId) {
        return res.status(400).json({ message: 'University selection is required' });
      }
      if (!studentId) {
        return res.status(400).json({ message: 'Student ID is required' });
      }
      if (!grade) {
        return res.status(400).json({ message: 'Grade is required' });
      }
      
      // Check if university exists
      const [university] = await db.promise().query(
        'SELECT * FROM universities WHERE id = ?',
        [universityId]
      );
      
      if (university.length === 0) {
        return res.status(400).json({ message: 'Selected university does not exist' });
      }
    }

    // Hash password
    const hashedPassword = await bcrypt.hash(password, 10);

    // Insert new user
    const [result] = await db.promise().query(
      'INSERT INTO users (username, email, password, user_type, university_id, student_id, grade, is_approved) VALUES (?, ?, ?, ?, ?, ?, ?, ?)',
      [username, email, hashedPassword, userType, universityId || null, studentId || null, grade || null, false]
    );

    res.status(201).json({ message: 'User registered successfully. Waiting for admin approval.' });
  } catch (error) {
    console.error('Registration error:', error);
    res.status(500).json({ message: 'Registration failed' });
  }
});

// Login endpoint
app.post('/api/login', async (req, res) => {
  const { email, password } = req.body;
  console.log('로그인 시도:', { email });  // 디버깅 로그 추가

  try {
    const [users] = await db.promise().query(
      'SELECT u.*, uni.name as university_name FROM users u LEFT JOIN universities uni ON u.university_id = uni.id WHERE u.email = ?',
      [email]
    );
    
    console.log('사용자 조회 결과:', users.length > 0 ? '사용자 찾음' : '사용자 없음');  // 디버깅 로그 추가

    if (users.length === 0) {
      return res.status(401).json({ message: 'Invalid credentials' });
    }

    const user = users[0];

    // Check if user is approved
    if (!user.is_approved) {
      console.log('승인되지 않은 사용자');  // 디버깅 로그 추가
      return res.status(403).json({ message: 'Account pending approval' });
    }

    // Verify password
    const validPassword = await bcrypt.compare(password, user.password);
    console.log('비밀번호 검증:', validPassword ? '일치' : '불일치');  // 디버깅 로그 추가
    
    if (!validPassword) {
      return res.status(401).json({ message: 'Invalid credentials' });
    }

    // Generate JWT token
    const token = jwt.sign(
      { 
        id: user.id, 
        username: user.username, 
        userType: user.user_type,
        universityId: user.university_id,
        studentId: user.student_id,
        grade: user.grade
      },
      JWT_SECRET,
      { expiresIn: '24h' }
    );

    console.log('로그인 성공:', user.username);  // 디버깅 로그 추가

    res.json({ 
      token,
      user: {
        id: user.id,
        username: user.username,
        email: user.email,
        userType: user.user_type,
        universityId: user.university_id,
        universityName: user.university_name,
        studentId: user.student_id,
        grade: user.grade
      }
    });
  } catch (error) {
    console.error('로그인 에러:', error);
    res.status(500).json({ message: 'Login failed' });
  }
});

// Forgot password endpoint
app.post('/api/forgot-password', async (req, res) => {
  const { email } = req.body;
  
  try {
    // Check if user exists
    const [users] = await db.promise().query(
      'SELECT * FROM users WHERE email = ?',
      [email]
    );
    
    if (users.length === 0) {
      // For security reasons, don't reveal that email doesn't exist
      return res.json({ message: 'If your email exists in our system, you will receive a password reset link.' });
    }

    // In a real application, generate a reset token and send email
    // For this demo, we'll just return success
    res.json({ message: 'If your email exists in our system, you will receive a password reset link.' });
  } catch (error) {
    console.error('Forgot password error:', error);
    res.status(500).json({ message: 'Failed to process request' });
  }
});

// Reset password endpoint
app.post('/api/reset-password', async (req, res) => {
  const { email, code, newPassword } = req.body;
  
  try {
    // In a real application, verify the reset code
    // For this demo, we'll accept any code (for testing purposes, use "123456")
    if (code !== '123456') {
      return res.status(400).json({ message: 'Invalid reset code' });
    }
    
    // Find user by email
    const [users] = await db.promise().query(
      'SELECT * FROM users WHERE email = ?',
      [email]
    );
    
    if (users.length === 0) {
      return res.status(404).json({ message: 'User not found' });
    }
    
    // Hash new password
    const hashedPassword = await bcrypt.hash(newPassword, 10);
    
    // Update user's password
    await db.promise().query(
      'UPDATE users SET password = ? WHERE email = ?',
      [hashedPassword, email]
    );
    
    res.json({ message: 'Password reset successfully' });
  } catch (error) {
    console.error('Reset password error:', error);
    res.status(500).json({ message: 'Failed to reset password' });
  }
});

// Admin endpoints
app.get('/api/admin/pending-users', authenticateToken, async (req, res) => {
  if (req.user.userType !== 'admin') {
    return res.status(403).json({ message: 'Access denied' });
  }

  try {
    const [users] = await db.promise().query(`
      SELECT u.id, u.username, u.email, u.user_type, u.student_id, u.grade, u.is_approved, uni.name as university_name 
      FROM users u 
      LEFT JOIN universities uni ON u.university_id = uni.id 
      WHERE u.is_approved = false
    `);
    res.json(users);
  } catch (error) {
    console.error('Error fetching pending users:', error);
    res.status(500).json({ message: 'Failed to fetch pending users' });
  }
});

app.post('/api/admin/approve-user/:userId', authenticateToken, async (req, res) => {
  if (req.user.userType !== 'admin') {
    return res.status(403).json({ message: 'Access denied' });
  }

  try {
    await db.promise().query(
      'UPDATE users SET is_approved = true WHERE id = ?',
      [req.params.userId]
    );
    res.json({ message: 'User approved successfully' });
  } catch (error) {
    console.error('Error approving user:', error);
    res.status(500).json({ message: 'Failed to approve user' });
  }
});

// Protected route example
app.get('/api/protected', authenticateToken, (req, res) => {
  res.json({ message: 'This is a protected route', user: req.user });
});

// WebSocket Handlers
async function handleUnityLogin(ws, data) {
    try {
        console.log('Received login data:', data);
        console.log('Data type:', typeof data);
        console.log('Data length:', data ? data.length : 0);
        
        // Make sure the WebSocket is still open
        if (ws.readyState !== 1) {
            console.error('WebSocket not open during login processing');
            return;
        }
        
        let loginData;
        try {
            loginData = JSON.parse(data);
            console.log('Parsed login data:', loginData);
        } catch (parseError) {
            console.error('JSON parse error:', parseError);
            console.error('Raw data:', data);
            if (ws.readyState === 1) {
                ws.send(JSON.stringify({ error: 'Invalid JSON format' }));
            }
            return;
        }

        console.log('Unity login attempt:', loginData.email);

        const [users] = await db.promise().query(
            'SELECT u.*, uni.name as university_name FROM users u LEFT JOIN universities uni ON u.university_id = uni.id WHERE u.email = ?',
            [loginData.email]
        );

        // Check WebSocket again
        if (ws.readyState !== 1) {
            console.error('WebSocket closed during database query');
            return;
        }

        if (users.length === 0) {
            console.log('User not found:', loginData.email);
            ws.send(JSON.stringify({ error: 'Invalid credentials' }));
            return;
        }

        const user = users[0];
        const validPassword = await bcrypt.compare(loginData.password, user.password);

        // Check WebSocket again
        if (ws.readyState !== 1) {
            console.error('WebSocket closed during password verification');
            return;
        }

        if (!validPassword) {
            console.log('Invalid password for user:', loginData.email);
            ws.send(JSON.stringify({ error: 'Invalid credentials' }));
            return;
        }

        if (!user.is_approved) {
            console.log('User not approved:', loginData.email);
            ws.send(JSON.stringify({ error: 'Account pending approval' }));
            return;
        }

        try {
            // Update user's online status
            await db.promise().query(
                'UPDATE users SET is_online = 1, last_activity = CURRENT_TIMESTAMP WHERE id = ?',
                [user.id]
            );
        } catch (dbError) {
            console.error('Error updating user online status:', dbError);
            // Continue anyway - this is not critical
        }

        // Check WebSocket again
        if (ws.readyState !== 1) {
            console.error('WebSocket closed during token generation');
            return;
        }

        const token = jwt.sign(
            { 
                id: user.id, 
                username: user.username, 
                userType: user.user_type,
                universityId: user.university_id,
                studentId: user.student_id,
                grade: user.grade
            },
            JWT_SECRET,
            { expiresIn: '24h' }
        );

        console.log('Login successful for user:', user.username);
        
        const response = { 
            token,
            user: {
                id: user.id,
                username: user.username,
                email: user.email,
                userType: user.user_type,
                universityId: user.university_id,
                universityName: user.university_name,
                studentId: user.student_id,
                grade: user.grade
            }
        };
        
        console.log('Sending response:', JSON.stringify(response));
        
        // Final WebSocket check before sending
        if (ws.readyState === 1) {
            ws.send(JSON.stringify(response));
            console.log('Login response sent successfully');
        } else {
            console.error('WebSocket closed, could not send login response');
        }
    } catch (error) {
        console.error('Unity login error:', error);
        if (ws.readyState === 1) {
            ws.send(JSON.stringify({ error: 'Login failed' }));
        }
    }
}

function handleTokenGeneration(ws, dataSet) {
    try {
        const tokenData = JSON.parse(dataSet);
        const channel = tokenData.channel;
        const uid = 0;
        
        if (!channel || uid == null) {
            return ws.send(JSON.stringify({ error: 'Missing channel or uid' }));
        }

        const role = Role.PUBLISHER;
        const expireTime = 3600;
        const currentTimestamp = Math.floor(Date.now() / 1000);
        const expireTimestamp = currentTimestamp + expireTime;
        
        const token = RtcTokenBuilder.buildTokenWithUid(
            appID,
            appCertificate,
            channel,
            uid,
            role,
            expireTimestamp
        );

        ws.send(JSON.stringify({token}));
        addLogToExcel(tokenData.uid + " 토큰 생성 - " + token);
    } catch (err) {
        console.error('Token creation error:', err);
        ws.send(JSON.stringify({ error: 'Token generation failed', details: err.message }));
    }
}

// Online Status Check
async function checkOnlineStatus() {
    try {
        // First check if the column exists
        const [columns] = await db.promise().query(`
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'users' 
            AND COLUMN_NAME = 'last_activity'
        `);

        if (columns.length === 0) {
            // Add the column if it doesn't exist
            await db.promise().query(`
                ALTER TABLE users 
                ADD COLUMN last_activity TIMESTAMP NULL,
                ADD COLUMN is_online BOOLEAN DEFAULT FALSE
            `);
            console.log('Added last_activity and is_online columns to users table');
            return;
        }

        const [users] = await db.promise().query('SELECT id, last_activity FROM users WHERE is_online = 1');
        const now = new Date();
        
        for (const user of users) {
            if (!user.last_activity) continue;
            
            const lastActivity = new Date(user.last_activity);
            const minutesDiff = (now - lastActivity) / (1000 * 60);
            
            if (minutesDiff > 3) {
                await db.promise().query(
                    'UPDATE users SET is_online = 0, last_activity = NULL WHERE id = ?',
                    [user.id]
                );
                addLogToExcel(`User ${user.id} marked as offline due to inactivity`);
            }
        }
    } catch (error) {
        console.error('Error checking online status:', error);
    }
}

// Start online status check interval
setInterval(checkOnlineStatus, 60000);

async function handleCustomGet(ws, data) {
    try {
        const customGet = require('./Custom/CustomGet');
        customGet(ws, db, data);
    } catch (error) {
        console.error('CustomGet 핸들러 오류:', error);
        ws.send(JSON.stringify({ error: 'Internal server error' }));
    }
}

async function handlePosGet(ws, data) {
    try {
        const posData = JSON.parse(data);
        const [users] = await db.promise().query(
            'SELECT pos_x, pos_y, pos_z, map_name FROM users WHERE email = ?',
            [posData.email]
        );

        if (users.length === 0) {
            ws.send(JSON.stringify({ error: 'User not found' }));
            return;
        }

        ws.send(JSON.stringify({
            posPlayerX: users[0].pos_x || 0,
            posPlayerY: users[0].pos_y || 0,
            posPlayerZ: users[0].pos_z || 0,
            map_name: users[0].map_name || ''
        }));
    } catch (error) {
        console.error('Pos get error:', error);
        ws.send(JSON.stringify({ error: 'Failed to get position data' }));
    }
}

async function handlePosSet(ws, data) {
    try {
        const posData = JSON.parse(data);
        await db.promise().query(
            'UPDATE users SET pos_x = ?, pos_y = ?, pos_z = ?, map_name = ?, last_activity = CURRENT_TIMESTAMP WHERE email = ?',
            [posData.posPlayerX, posData.posPlayerY, posData.posPlayerZ, posData.map_name, posData.email]
        );
        ws.send(JSON.stringify({ success: true }));
    } catch (error) {
        console.error('Pos set error:', error);
        ws.send(JSON.stringify({ error: 'Failed to update position' }));
    }
}

async function handleCustomChange(ws, data) {
    try {
        const customSet = require('./Custom/CustomSet');
        customSet(ws, db, data);
    } catch (error) {
        console.error('CustomChange 핸들러 오류:', error);
        ws.send(JSON.stringify({ error: 'Internal server error' }));
    }
}

async function handleInventoryGet(ws, data) {
    try {
        const inventoryData = JSON.parse(data);
        const [users] = await db.promise().query(
            'SELECT inventory_data FROM users WHERE email = ?',
            [inventoryData.email]
        );

        if (users.length === 0) {
            ws.send(JSON.stringify({ error: 'User not found' }));
            return;
        }

        ws.send(JSON.stringify(users[0].inventory_data || {}));
    } catch (error) {
        console.error('Inventory get error:', error);
        ws.send(JSON.stringify({ error: 'Failed to get inventory data' }));
    }
}

async function handleInventorySet(ws, data) {
    try {
        const inventoryData = JSON.parse(data);
        await db.promise().query(
            'UPDATE users SET inventory_data = ?, last_activity = CURRENT_TIMESTAMP WHERE email = ?',
            [JSON.stringify(inventoryData), inventoryData.email]
        );
        ws.send(JSON.stringify({ success: true }));
    } catch (error) {
        console.error('Inventory set error:', error);
        ws.send(JSON.stringify({ error: 'Failed to update inventory' }));
    }
}

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
  console.log(`HTTP Server running on port ${PORT}`);
  console.log(`WebSocket Server running on port 9000`);
});
