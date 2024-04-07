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
        (a.RIGHTBOUND < {2}) AND
        (a.LEFTBOUND > {3}) AND
        (a.TOPBOUND < {4}) AND
        (a.BOTTOMBOUND > {5})
    )
)
WHERE rn > {1}