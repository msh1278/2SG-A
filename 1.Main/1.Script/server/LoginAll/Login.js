
const addLogToExcel = require('../logUse.js');

module.exports = function(ws,conn,dataSet) {
    data = JSON.parse(dataSet);
    //addLogToExcel(data.stu_number+"로그인 시도");
    addLogToExcel(data.stu_number+"try(login)");

    //query = 'SELECT COUNT(*) AS count,stu_name FROM stu_data WHERE stu_number = ? AND stu_password = ?;';
    query = 'SELECT COUNT(*) AS count,username AS stu_name FROM users WHERE email = ? AND password = ?;'; // 내용 변경 (패스워드 패턴 확인 후 변경 필요)

    conn.query(query, [data.stu_number, data.stu_password], (err, results) => {
        if (err) {
            ws.send(0);
            addLogToExcel('쿼리 실행 실패: ' + err.stack);//?? 원래 불가능
            return;
        }
        //ws.send(results[0].count);//유저 네임으로 반환 
        if(results[0].count != 0)
        {
            ws.send(results[0].stu_name);
        }
        else
        {
            ws.send(0);
        }

        if(results[0].count == 1)//로그인 성공
        {
            //addLogToExcel("(온라인 정보) 확인 : "+ data.stu_number);
            addLogToExcel(data.stu_number+"try(login check)");
            query = 'SELECT COUNT(*) AS count FROM stu_online WHERE stu_number = ?;';
            conn.query(query, [data.stu_number], (err, results) => {
            if (err) {
                //ws.send(0);
                //addLogToExcel('쿼리 실행 실패: ' + err.stack);//?? 원래 불가능
                return;
            }
    
            //console.log(results[0].count);
            if(results[0].count == 0) //존재 하지 않음
            {
                addLogToExcel("(온라인 정보) 존재 하지 않음"+ data.stu_number);
                query = 'INSERT INTO stu_online (stu_number, stu_local_code, online,time, map_name,posPlayerX,posPlayerY,posPlayerZ,modelName,modelNum) VALUES ( ?, ? , 1 , ? ,"",0,0,0,"model_1",0)';
    
                conn.query(query, [data.stu_number,data.stu_local_code,data.time], (err, results) => {
                    if (err) {
                        addLogToExcel("(온라인 정보) 데이터 생성 실패 : "+ data.stu_number);
                        //ws.send(0);
                        return;
                    }
                    addLogToExcel("(온라인 정보) 데이터 생성 성공 : "+ data.stu_number);
                    //ws.send(1); //성공 1
                });
            }
            else
            {
                const updateQuery = 'UPDATE stu_online SET online = 1, stu_local_code = ?,time = ? WHERE stu_number = ?';

                conn.query(updateQuery, [data.stu_local_code,data.time,data.stu_number], (err, results) => {
                    if (err) {
                        addLogToExcel("(온라인 정보) 데이터 수정 실패 : " + data.stu_number);
                        // ws.send(0);
                        return;
                    }
                    addLogToExcel("(온라인 정보) 데이터 수정 성공 : " + data.stu_number);
                    // ws.send(1); //성공 1
                });
            }
            });
        }
    });
    

}