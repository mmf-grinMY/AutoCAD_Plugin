SELECT * FROM 
( 
    SELECT 
    a.drawjson, a.geowkt, a.systemid, a.paramjson, a.objectguid,
    b.layername, b.sublayername, b.basename, b.childfields,
    ROWNUM As rn
    FROM k{0}_trans_clone a 
    JOIN k{0}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid
    WHERE a.geowkt IS NOT NULL AND 
    (
        NOT (a.RIGHTBOUND < {3}) AND
        NOT (a.LEFTBOUND > {2}) AND
        NOT (a.TOPBOUND < {5}) AND
        NOT (a.BOTTOMBOUND > {4})
    )
)
WHERE rn > {1}