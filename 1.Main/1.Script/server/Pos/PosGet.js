
const addLogToExcel = require('../logUse.js');

module.exports = function(ws,conn,dataSet) {
    data = JSON.parse(dataSet);
    addLogToExcel(data.stu_number+"위치 정보 불러오기 시도");

    query = 'SELECT map_name,posPlayerX,posPlayerY FROM stu_online WHERE stu_number = ?;';

    conn.query(query, [data.stu_number], (err, results) => {
        if (err) {
            ws.send(0);
            addLogToExcel('쿼리 실행 실패: ' + err.stack);//?? 원래 불가능
            return;
        }
        
        ws.send(JSON.stringify(results[0]));
    });
    

}