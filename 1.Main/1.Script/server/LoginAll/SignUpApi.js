
const addLogToExcel = require('../logUse.js');

module.exports = function(ws,conn,data) {
    data2 = JSON.parse(data);
    addLogToExcel(data2.stu_number+" 회원가입 시도");

    //var sql = 'select * from userdata'

    query = 'INSERT INTO stu_data (stu_number, stu_name, stu_password) VALUES ( ?, ?, ? )';

    conn.query(query, [data2.stu_number, data2.stu_name, data2.stu_password], (err, results) => {
        if (err) {
            addLogToExcel("(유저 정보) 데이터 생성 실패 : "+ JSON.stringify(data));
            ws.send(0);
            return;
        }
        addLogToExcel("(유저 정보) 데이터 생성 성공 : "+ JSON.stringify(data));
        ws.send(1); //성공 1
    });
    
    query = 'SELECT COUNT(*) AS count FROM stu_online WHERE stu_number = ?;';

    conn.query(query, [data2.stu_number], (err, results) => {
        if (err) {
            //ws.send(0);
            //addLogToExcel('쿼리 실행 실패: ' + err.stack);//?? 원래 불가능
            return;
        }

        //console.log(results[0].count);
        if(results[0].count == 0) //존재 하지 않음
        {
            addLogToExcel("(온라인 정보) 존재 하지 않음"+ data2.stu_number);
            query = 'INSERT INTO stu_online (stu_number, stu_local_code, online, time, map_name,posPlayerX,posPlayerY,posPlayerZ,modelName,modelNum) VALUES ( ?, 0, 0, 0 , "",0,0,0,"model_1",0)';

            conn.query(query, [data2.stu_number], (err, results) => {
                if (err) {
                    addLogToExcel("(온라인 정보) 데이터 생성 실패 : "+ data2.stu_number);
                    //ws.send(0);
                    return;
                }
                addLogToExcel("(온라인 정보) 데이터 생성 성공 : "+ data2.stu_number);
                //ws.send(1); //성공 1
            });
        }
    });
}
