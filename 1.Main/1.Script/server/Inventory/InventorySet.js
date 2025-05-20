const addLogToExcel = require('../logUse.js');

module.exports = function(ws, conn, dataSet) {
    // 데이터 파싱
    const data = JSON.parse(dataSet);
    addLogToExcel(data.stu_number + " 아이템 정보 수정");

    // 먼저 해당 student_num과 item_name에 대한 기존 아이템이 있는지 확인
    const selectQuery = 'SELECT * FROM stu_item WHERE student_num = ? AND item_name = ?';

    conn.query(selectQuery, [data.stu_number, data.item_name], (err, results) => {
        if (err) {
            addLogToExcel("(아이템 정보) 조회 실패 : " + data.stu_number);
            ws.send(0);
            return;
        }

        // 아이템이 존재하면
        if (results.length > 0) {
            // item_count를 업데이트하고, slot_Num이 다르면 변경
            const existingItem = results[0];

           // slot_Num이 다르면 업데이트
           const updateQuery = 'UPDATE stu_item SET item_count = ?, slot_Num = ? WHERE student_num = ? AND item_name = ?';
           conn.query(updateQuery, [existingItem.item_count + data.item_count, data.slot_Num, data.stu_number, data.item_name], (err, results) => {
               if (err) {
                   addLogToExcel("(아이템 정보) 업데이트 실패 : " + data.stu_number);
                   ws.send(0);
                   return;
               }
               addLogToExcel("(아이템 정보) 업데이트 성공 : " + data.stu_number);
               ws.send(1);
           });
        } 
        else 
        {
            // 아이템이 없으면 새로운 레코드 추가
            const insertQuery = 'INSERT INTO stu_item (student_num, item_name, item_count, slot_Num) VALUES (?, ?, ?, ?)';
            conn.query(insertQuery, [data.stu_number, data.item_name, data.item_count, data.slot_Num], (err, results) => {
                if (err) {
                    addLogToExcel("(아이템 정보) 추가 실패 : " + data.stu_number);
                    ws.send(0);
                    return;
                }
                addLogToExcel("(아이템 정보) 추가 성공 : " + data.stu_number);
                ws.send(1);
            });
        }

        // item_count가 0이면 레코드 삭제
        const deleteQuery = 'DELETE FROM stu_item WHERE student_num = ? AND item_count <= 0';
        conn.query(deleteQuery, [data.stu_number], (err, results) => {
            if (err) {
                addLogToExcel("(아이템 정보) 삭제 실패 : " + data.stu_number);
                ws.send(0);
            } else {
                addLogToExcel("(아이템 정보) 삭제 성공 : " + data.stu_number);
                ws.send(1);
            }
        });
    });
};