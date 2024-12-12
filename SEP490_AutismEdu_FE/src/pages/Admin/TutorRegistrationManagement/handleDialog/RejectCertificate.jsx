import { Box, Button, Modal, TextField, Typography } from '@mui/material'
import { enqueueSnackbar } from 'notistack';
import React, { useState } from 'react'
import LoadingComponent from '~/components/LoadingComponent'
import { useTutorContext } from '~/Context/TutorContext';
import services from '~/plugins/services';

function RejectCertificate({ id, certificateId, setCertificates, certificates }) {
    const [loading, setLoading] = useState(false);
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [rejectReason, setRejectReason] = useState("");
    const handleSubmit = async () => {
        try {
            setLoading(true);
            if (rejectReason === "") {
                setLoading(false);
                enqueueSnackbar("Bạn chưa nhập lý do từ chối chứng chỉ", { variant: "error" })
                return;
            }
            await services.CertificateAPI.changeStatusCertificate(certificateId,
                {
                    id: certificateId,
                    statusChange: 0,
                    rejectionReason: rejectReason
                },
                (res) => {
                    const updatedCertificates = certificates.map((c) => {
                        return c.id === res.result.id ? res.result : c
                    })
                    setCertificates([...updatedCertificates]);
                    enqueueSnackbar("Cập nhật thành công!", { variant: "success" })
                }, (err) => {
                    console.log(err);
                    enqueueSnackbar("Lỗi hệ thống!", { variant: "error" })
                }, {
                id: id
            });
            setLoading(false);
            handleClose();
        } catch (error) {
            setLoading(false);
        }
    }
    return (
        <>
            <Button onClick={handleOpen}>Từ chối</Button>
            <Modal
                open={open}
                onClose={handleClose}
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
                        Bạn muốn từ chối chứng chỉ này ?
                    </Typography>
                    <Typography mt={3}>Lý do</Typography>
                    <TextField
                        multiline
                        rows={8}
                        fullWidth
                        value={rejectReason}
                        onChange={(e) => { setRejectReason(e.target.value) }}
                    />
                    <Box textAlign="right" mt={2}>
                        <Button onClick={handleClose}>Huỷ bỏ</Button>
                        <Button onClick={handleSubmit}>Từ chối</Button>
                    </Box>
                    <LoadingComponent open={loading} setLoading={setLoading} />
                </Box>
            </Modal>
        </>
    )
}

export default RejectCertificate
