const addLogToExcel = require('../logUse.js');

module.exports = function(ws, conn, dataSet) {
    try {
        const data = JSON.parse(dataSet);
        addLogToExcel(data.email + " 커스텀 정보 저장 시도");

        // 데이터 유효성 검사
        if (!data.email) {
            console.error('CustomSet: 이메일이 없습니다');
            ws.send(JSON.stringify({ error: 'Email is required' }));
            return;
        }

        // custom_data 형식 검증 및 기본값 설정
        const customData = {
            modelName: data.modelName || 'model_1',
            customNum: parseInt(data.customNum) || 0
        };

        // custom_data가 유효한지 확인
        if (typeof customData.modelName !== 'string' || customData.modelName.trim() === '') {
            customData.modelName = 'model_1';
        }

        if (isNaN(customData.customNum) || customData.customNum < 0) {
            customData.customNum = 0;
        }

        // 먼저 사용자가 존재하는지 확인
        conn.query('SELECT id FROM users WHERE email = ?', [data.email], (err, results) => {
            if (err) {
                console.error('CustomSet 사용자 확인 실패:', err);
                ws.send(JSON.stringify({ error: 'Database error' }));
                addLogToExcel('쿼리 실행 실패: ' + err.stack);
                return;
            }

            if (!results || results.length === 0) {
                console.error('CustomSet: 사용자를 찾을 수 없음');
                ws.send(JSON.stringify({ error: 'User not found' }));
                return;
            }

            // custom_data 업데이트
            const updateQuery = `
                UPDATE users 
                SET custom_data = ?, 
                    last_activity = CURRENT_TIMESTAMP 
                WHERE email = ?
            `;

            conn.query(updateQuery, [JSON.stringify(customData), data.email], (updateErr, updateResults) => {
                if (updateErr) {
                    console.error('CustomSet 업데이트 실패:', updateErr);
                    ws.send(JSON.stringify({ error: 'Failed to update custom data' }));
                    addLogToExcel('업데이트 실패: ' + updateErr.stack);
                    return;
                }

                if (updateResults.affectedRows === 0) {
                    console.error('CustomSet: 업데이트된 행이 없음');
                    ws.send(JSON.stringify({ error: 'Update failed' }));
                    return;
                }

                // 성공 응답
                const response = {
                    success: true,
                    email: data.email,
                    modelName: customData.modelName,
                    customNum: customData.customNum
                };

                console.log('CustomSet 성공:', response);
                ws.send(JSON.stringify(response));
                addLogToExcel(data.email + ' 커스텀 정보 저장 성공');
            });
        });
    } catch (error) {
        console.error('CustomSet 처리 실패:', error);
        ws.send(JSON.stringify({ error: 'Invalid request' }));
        addLogToExcel('CustomSet 처리 실패: ' + error.message);
    }
}; 