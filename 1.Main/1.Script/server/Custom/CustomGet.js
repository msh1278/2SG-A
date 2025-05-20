const addLogToExcel = require('../logUse.js');

module.exports = function(ws, conn, dataSet) {
    try {
        const data = JSON.parse(dataSet);
        addLogToExcel(data.email + " 커스텀 정보 불러오기 시도");

        // users 테이블에서 custom_data 조회
        const query = 'SELECT custom_data FROM users WHERE email = ?';
        
        conn.query(query, [data.email], (err, results) => {
            if (err) {
                console.error('CustomGet 쿼리 실행 실패:', err);
                ws.send(JSON.stringify({ error: 'Database error' }));
                addLogToExcel('쿼리 실행 실패: ' + err.stack);
                return;
            }

            if (!results || results.length === 0) {
                console.error('CustomGet: 사용자를 찾을 수 없음');
                ws.send(JSON.stringify({ error: 'User not found' }));
                return;
            }

            try {
                // custom_data가 JSON 문자열인 경우 파싱
                let customData = results[0].custom_data;
                if (typeof customData === 'string') {
                    // 기본값이 'model_1'인 경우 처리
                    if (customData === 'model_1') {
                        customData = {
                            modelName: 'model_1',
                            customNum: 0
                        };
                    } else {
                        try {
                            customData = JSON.parse(customData);
                        } catch (e) {
                            console.error('Custom data parse error:', e);
                            customData = {
                                modelName: 'model_1',
                                customNum: 0
                            };
                        }
                    }
                }

                // 응답 형식 맞추기
                const response = {
                    email: data.email,
                    modelName: customData.modelName || 'model_1',
                    customNum: customData.customNum || 0
                };

                console.log('CustomGet 응답:', response);
                ws.send(JSON.stringify(response));
                addLogToExcel(data.email + ' 커스텀 정보 불러오기 성공');
            } catch (error) {
                console.error('CustomGet 데이터 처리 실패:', error);
                ws.send(JSON.stringify({ 
                    email: data.email,
                    modelName: 'model_1',
                    customNum: 0
                }));
            }
        });
    } catch (error) {
        console.error('CustomGet 처리 실패:', error);
        ws.send(JSON.stringify({ error: 'Invalid request' }));
        addLogToExcel('CustomGet 처리 실패: ' + error.message);
    }
};