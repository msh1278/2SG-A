
const addLogToExcel = require('../logUse.js');

module.exports = function(ws,conn,dataSet) {
    data = JSON.parse(dataSet);
    addLogToExcel(data.stu_number+"커스텀 정보 수정");

    const updateQuery = 'UPDATE stu_online SET modelName = ?,modelNum = ? WHERE stu_number = ?';

    conn.query(updateQuery, [data.modelName,data.modelNum,data.stu_number], (err, results) => {
        if (err) {
            addLogToExcel("(커스텀 정보) 데이터 수정 실패 : " + data.stu_number);
            ws.send(0);
            return;
        }
        addLogToExcel("(커스텀 정보) 데이터 수정 성공 : " + data.stu_number);
        ws.send(1); //성공 1
    });
    

}