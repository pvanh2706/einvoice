# MISA FormSerial fixture

Synthetic input, no provider calls or production data. Expected values were derived from pinned
`66a9c742d0f6fb692d339076e6cd82780e3476be`:
`MisaConvertResponse.ConvertInvoiceFormSerial` and `ResponseModel.FormSerialResponse`.

The fixture checks inherited-template filtering **before** name access, the 40 UTF-16 code unit
name limit, inactive/active status, BusinessAreas 34 and unmapped zero/null defaults.
Tests wrap the same provider array as normal JSON-in-Data, a nested JSON string and legacy escaped
or quoted arrays. Separate tests preserve escaped quotes in names, which the legacy blanket
Replace would corrupt. This is an offline regression fixture, not a sandbox/production golden response.
