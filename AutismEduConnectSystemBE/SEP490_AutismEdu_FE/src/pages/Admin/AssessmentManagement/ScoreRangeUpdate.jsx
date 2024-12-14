import * as React from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Modal from '@mui/material/Modal';
import { useFormik } from 'formik';
import { FormHelperText, IconButton, Stack, TextField } from '@mui/material';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import EditIcon from '@mui/icons-material/Edit';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 700,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4
};

export default function ScoreRangeUpdate({ scoreRanges, setScoreRanges, currentScoreRange }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const validate = values => {
        const errors = {}
        if (!values.description) {
            errors.description = "Bắt buộc";
        }
        if (!values.minScore) {
            errors.minScore = "Bắt buộc";
        }
        if (!values.maxScore) {
            errors.maxScore = "Bắt buộc";
        }
        if (Number(values.minScore) > Number(values.maxScore)) {
            errors.minScore = "Khoảng điểm không hợp lệ!";
        }
        return errors
    }
    const formik = useFormik({
        initialValues: {
            description: '',
            minScore: '',
            maxScore: ''
        }, validate,
        onSubmit: async (values) => {
            if (scoreRanges && scoreRanges.length !== 0) {
                const existSR = scoreRanges.find((s) => {
                    return s.minScore === values.minScore && s.maxScore === values.maxScore;
                })
                if (existSR) {
                    enqueueSnackbar("Đã tồn tại khung điểm này", { variant: "error" });
                    return;
                }
            }
            try {
                await services.ScoreRangeAPI.updateScoreRange({
                    ...values,
                    id: currentScoreRange.id
                }, (res) => {
                    const updatedArr = scoreRanges.map((s) => {
                        if (s.id === currentScoreRange.id) {
                            return res.result
                        } else {
                            return s
                        }
                    })
                    setScoreRanges(updatedArr);
                    enqueueSnackbar("Cập nhật thành công", { variant: "success" });
                    formik.resetForm();
                    handleClose();
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                })
            } catch (error) {
                enqueueSnackbar("Cập nhật thất bại", { variant: "error" })
            }
        }
    })
    React.useEffect(() => {
        if (currentScoreRange) {
            formik.resetForm({
                values: {
                    description: currentScoreRange.description || "",
                    minScore: currentScoreRange.minScore || 0,
                    maxScore: currentScoreRange.maxScore || ""
                }
            })
        }
    }, [currentScoreRange])
    return (
        <>
            <IconButton
                sx={{ color: '#787bff' }}
                onClick={handleOpen}
            ><EditIcon /></IconButton>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={style}>
                    <Typography variant="h6" component="h2" mb={3}>
                        Cập nhật đánh giá
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Typography >Mô tả</Typography>
                        <TextField
                            multiline
                            rows={5}
                            fullWidth
                            name='description'
                            value={formik.values.description}
                            onChange={formik.handleChange}
                        />
                        <Typography sx={{ textAlign: "right", fontSize: "12px" }}>{formik.values.description.length} / 300</Typography>
                        {
                            formik.errors.description && (
                                <FormHelperText error>
                                    {formik.errors.description}
                                </FormHelperText>
                            )
                        }
                        <Typography mt={3}>Khoảng điểm</Typography>
                        <Stack direction='row' mt={2}>
                            <Stack direction='row' sx={{ width: "50%" }} alignItems="center" gap={2}>
                                <Typography>Từ: </Typography>
                                <TextField
                                    name='minScore'
                                    onChange={formik.handleChange}
                                    value={formik.values.minScore}
                                    type='Number'
                                    sx={{ width: "70%" }}
                                    inputProps={{
                                        min: 0,
                                        max: 500
                                    }}
                                />
                            </Stack>
                            <Stack direction='row' sx={{ width: "50%" }} alignItems="center" gap={2}>
                                <Typography>Đến: </Typography>
                                <TextField
                                    name='maxScore'
                                    onChange={formik.handleChange}
                                    value={formik.values.maxScore}
                                    type='Number'
                                    sx={{ width: "70%" }}
                                    inputProps={{
                                        min: 0,
                                        max: 500
                                    }}
                                />
                            </Stack>
                        </Stack>
                        <Stack direction='row'>
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.minScore && (
                                        <FormHelperText error>
                                            {formik.errors.minScore}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.maxScore && (
                                        <FormHelperText error>
                                            {formik.errors.maxScore}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                        </Stack>
                        <Button variant='contained' type='submit' sx={{ mt: 2 }}>Tạo nhận xét</Button>
                    </form>
                </Box>
            </Modal>
        </>
    );
}