import { Box, Modal, Typography } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import TextField from '@mui/material/TextField';
import { enqueueSnackbar } from 'notistack';
import { useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';

function AcceptDialog({ id, status, setTutorInformation, setCurriculums, setCertificates, setWorkExperiences }) {
    const [open, setOpen] = useState(false);
    const [rejectReason, setRejectReason] = useState("");
    const [loading, setLoading] = useState(false);
    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handleSubmit = async () => {
        try {
            setLoading(true);
            if (status === 0 && rejectReason === "") {
                setLoading(false);
                enqueueSnackbar("Bạn chưa nhập lý do từ chối đơn đăng ký", { variant: "error" })
                return;
            } else if (status === 0 && rejectReason.length > 500) {
                setLoading(false);
                enqueueSnackbar("Lý do dưới 500 ký tự", { variant: "error" })
                return;
            }
            await services.TutorManagementAPI.handleRegistrationForm(id,
                {
                    id: id,
                    statusChange: status,
                    rejectionReason: rejectReason
                },
                (res) => {
                    const { certificates, curriculums, workExperiences, ...tutorInfo } = res.result
                    setTutorInformation(tutorInfo);
                    setCertificates(certificates);
                    setCurriculums(curriculums);
                    setWorkExperiences(workExperiences);
                    enqueueSnackbar("Cập nhật thành công!", { variant: "success" })
                }, (err) => {
                    enqueueSnackbar(err.error[0], { variant: "error" })
                }, {
                id: id
            });
            setLoading(false);
            handleClose();
        } catch (error) {
            enqueueSnackbar("Cập nhật thất bại!");
            setLoading(false);
        }
    }
    return (
        <>
            <Button sx={{ ml: 2 }} color={status === 1 ? "success" : "error"} variant='contained'
                onClick={handleClickOpen}
            >{status === 1 ? "Chấp nhận" : "Từ chối"}</Button>
            {
                status === 1 && (
                    <Dialog
                        open={open}
                        onClose={handleClose}
                    >
                        <DialogContent>
                            <DialogContentText>
                                Bạn có muốn {status === 1 ? "chấp nhận" : "từ chối"} đơn đăng ký này?
                            </DialogContentText>
                        </DialogContent>
                        <DialogActions>
                            <Button onClick={handleClose}>Huỷ bỏ</Button>
                            <Button onClick={handleSubmit}>Chấp nhận</Button>
                        </DialogActions>
                        <LoadingComponent open={loading} setLoading={setLoading} />
                    </Dialog>
                )
            }
            {
                status === 0 && (
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
                                Bạn muốn từ chối đơn đăng ký này ?
                            </Typography>
                            <Typography mt={3}>Lý do</Typography>
                            <TextField
                                multiline
                                rows={8}
                                fullWidth
                                value={rejectReason}
                                onChange={(e) => { setRejectReason(e.target.value) }}
                            />
                            <Typography sx={{ textAlign: "right" }}>{rejectReason.length} / 500</Typography>
                            <Box textAlign="right" mt={2}>
                                <Button onClick={handleClose}>Huỷ bỏ</Button>
                                <Button onClick={handleSubmit}>Từ chối</Button>
                            </Box>
                            <LoadingComponent open={loading} setLoading={setLoading} />
                        </Box>
                    </Modal>
                )
            }
        </>
    )
}

export default AcceptDialog
