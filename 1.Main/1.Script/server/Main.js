const WebSocket = require("ws");
const wss = new WebSocket.Server({ port: 8080 });

const mysql = require('mysql2');

/*
var conn = mysql.createConnection({ 
    host : 'localhost',  
    user : 'root',
    password : '',
    database : 'yjp'
});
*/

var conn = mysql.createConnection({
  host: process.env.DB_HOST || 'localhost',
  user: process.env.DB_USER || 'root',
  password: process.env.DB_PASSWORD || 'Emfprhsqhf1!',
  database: process.env.DB_NAME || 'yjp',
  charset: 'utf8mb4',
  collation: 'utf8mb4_unicode_ci'
});

conn.connect();


const SignUpApi = require('./LoginAll/SignUpApi.js');
const addLogToExcel = require('./logUse.js');
const Login = require("./LoginAll/Login.js");
const Upate = require("./LoginAll/Upate.js");
const Token = require("../Token.js");
const CustomChange = require("./Custom/CustomChange.js");
const CustomGet = require("./Custom/CustomGet.js");
const PosGet = require("./Pos/PosGet.js");
const PosSet = require("./Pos/PosSet.js");

const InventoryGet = require("./Inventory/InventoryGet.js");
const InventorySet = require("./Inventory/InventorySet.js");

wss.on("connection", ws => {
    addLogToExcel("Unity connected!");

    ws.on("message", message => {

        if (Buffer.isBuffer(message)) {
            message = message.toString();
        }

        let splitMsg = message.split("{****}");
        
        switch(splitMsg[0])
        {
            case "SignUp":
              //stu_data 테이블 사용 안함 (회원가입 방식 사이트로 변경)
              //SignUpApi(ws,conn,splitMsg[1]);
              break;
              
            case "Login":
              Login(ws,conn,splitMsg[1]);
              // 로그인 중이라는 표시 해줄것 DB에서
              break;

            case "Upate":
              Upate(ws,conn,splitMsg[1]);
              break;
            case "UidCheck":
              //data = JSON.parse(splitMsg[1]);
              var sql = 'SELECT stu_local_code FROM stu_online Where stu_number = ?';
              //console.log(splitMsg[1]);
              conn.query(sql, [splitMsg[1]], function(err, rows, fields)
              {
                if (err) 
                {
                  console.error('error connecting: ' + err.stack);
                  return;
                }
                ws.send(rows[0].stu_local_code);
                //console.log(rows[0].stu_local_code);
              });

              break;
              
            case "Token":
              Token(ws,splitMsg[1]);
              break;
            case "CustomChange":
              CustomChange(ws,conn,splitMsg[1]);
              break;
            case "CustomGet":
              CustomGet(ws,conn,splitMsg[1]);
              break;
            case "PosGet":
              PosGet(ws,conn,splitMsg[1]);
              break;
            case "PosSet":
              PosSet(ws,conn,splitMsg[1]);
              break;
            case "InventoryGet":
              InventoryGet(ws,conn,splitMsg[1]);

              break;

            case "InventorySet":
              InventorySet(ws,conn,splitMsg[1]);

              break;

        }
    });

    //ws.send("Connected to Node.js WebSocket Server");
});


run();

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function run() {
  //console.log("온라인 확인 시작");
  
  await delay(60000); //1분 대기

  //console.log("딜레이 후 작업 실행"); //시간 저장 후 3분 이상 지났으면 로그아웃 표시

  //60 - today.getMinutes()
  //60 - (temp)
  var sql = 'SELECT stu_number, time,online FROM stu_online';
  
  conn.query(sql, function(err, rows, fields)
  {
    if (err) 
    {
      console.error('error connecting: ' + err.stack);
      return;
    }

    for(i = 0; i < rows.length; i++)
    {
      const today = new Date();
      temp1 = 60 - today.getMinutes();
      temp2 = 60 - rows[i].time;
      if(temp1 > temp2)
      {
        if((temp1-temp2) > 3 && rows[i].online == 1) //3분 보다 크면
        {
          //오프라인 적용
          Delet(rows[i].stu_number);
        }
      }
      else
      {
        if((temp2-temp1) > 3 && rows[i].online == 1) //3분 보다 크면
        {
          //오프라인 적용
          Delet(rows[i].stu_number);
        }
      }

    }
  });
  run();//반복
}

async function Delet(str) 
{
  //data = JSON.parse(dataSet);
  const updateQuery = 'UPDATE stu_online SET online = 0,stu_local_code = null WHERE stu_number = ?';

  conn.query(updateQuery, [str], (err, results) => {
      if (err) {
          //addLogToExcel("(온라인 정보) 데이터 수정 실패 : " + data.stu_number);
          // ws.send(0);
          return;
      }
      addLogToExcel("데이터 오프라인 : " + str);
      // ws.send(1); //성공 1
  });
}

addLogToExcel("WebSocket server running on ws://127.0.0.1:8080");
//web 소켓 127.0.0.1

