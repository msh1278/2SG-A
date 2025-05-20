const addLogToExcel = require('../logUse.js');

module.exports = function(ws, conn, dataSet) {
    const data = JSON.parse(dataSet);
    addLogToExcel(data.stu_number + "인벤토리 아이템 정보 불러오기 시도");

    const query = 'SELECT student_num, item_name, item_count, slot_Num FROM stu_item WHERE student_num = ?;';

    conn.query(query, [data.stu_number], (err, results) => {
        if (err) {
            ws.send(0);
            return;
        }

        ws.send(JSON.stringify(results));
    });
}