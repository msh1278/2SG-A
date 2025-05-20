const { RtcTokenBuilder, Role } = require('./nodejs/src/RtcTokenBuilder2.js');
const addLogToExcel = require('./logUse.js');
const { json } = require('express');

const appID = "f86b34d0a03c48979cd2535c603157ce";
const appCertificate = "2728f4458b83423592dbe431a6741680";

module.exports = function(ws, dataSet) {
    try {
        const data = JSON.parse(dataSet);
        const channel = data.channel;
        const uid =  0; //UID를 접속시 사용하지 않을 경우 0 으로 고정 할것
        if (!channel || uid == null) {
            return ws.send(JSON.stringify({ error: 'Missing channel or uid' }));
        }

        const role = Role.PUBLISHER;
        const expireTime = 3600;
        
        const currentTimestamp = Math.floor(Date.now() / 1000);  // 현재 시각 (Unix time)
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
        addLogToExcel(data.uid+" 토큰 생성 - "+token);

    } catch (err) {
        console.error('Token creation error:', err);
        ws.send(JSON.stringify({ error: 'Token generation failed', details: err.message }));
    }
}
