import { Box, Modal, Typography } from '@mui/material';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { enqueueSnackbar } from 'notistack';
import { useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
function StatusChangeConfirm({ id, status, open, setOpen, setReport, report }) {
    const [reportResponse, setReportResponse] = useState("");
    const [loading, setLoading] = useState(false);
    const handleSubmit = async () => {
        try {
            setLoading(true);
            if (reportResponse === "") {
                setLoading(false);
                enqueueSnackbar("Bạn chưa nhập phản hồi", { variant: "error" })
                return;
            } else if (reportResponse.length > 500) {
                setLoading(false);
                enqueueSnackbar("Phản hồi dưới 500 ký tự", { variant: "error" })
                return;
            }
            await services.ReportManagementAPI.changeReportStatus(id,
                {
                    id: id,
                    statusChange: status,
                    comment: reportResponse
                },
                (res) => {
                    enqueueSnackbar("Cập nhật thành công!", { variant: "success" })
                    setReport({
                        ...report,
                        status: status
                    })
                    setOpen(false);
                }, (err) => {
                    enqueueSnackbar("Lỗi hệ thống!", { variant: "error" })
                }, {
                id: id
            });
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }
    return (
        <Modal
            open={open}
            onClose={() => setOpen(false)}
        >
            <Box sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                width: 600,
                bgcolor: 'background.paper',
                borderRadius: "10px",
                boxShadow: 24,
                p: 4
            }}>
                <Typography fontWeight="bold">
                    Bạn muốn {status === 1 ? "tiếp nhận" : "từ chối"} đơn tố cáo này ?
                </Typography>
                <Typography mt={3}>Phản hồi</Typography>
                <TextField
                    multiline
                    rows={8}
                    fullWidth
                    value={reportResponse}
                    onChange={(e) => { setReportResponse(e.target.value) }}
                />
                <Typography sx={{ textAlign: "right" }}>{reportResponse.length} / 500</Typography>
                <Box textAlign="right" mt={2}>
                    <Button onClick={() => setOpen(false)}>Huỷ bỏ</Button>
                    <Button onClick={handleSubmit}>{status === 1 ? "Tiếp nhận" : "Từ chối"}</Button>
                </Box>
                <LoadingComponent open={loading} setLoading={setLoading} />
            </Box>
        </Modal>
    )
}

export default StatusChangeConfirm
