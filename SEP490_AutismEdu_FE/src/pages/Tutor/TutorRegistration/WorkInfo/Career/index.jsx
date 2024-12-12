import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import { FormHelperText, Grid, IconButton, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import * as React from 'react';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 850,
    maxHeight: "90vh",
    bgcolor: 'background.paper',
    boxShadow: 24,
    overflowY: "auto",
    p: 4,
};

export default function Career({ career, setCareer }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => {
        setOpen(false);
        formik.resetForm();
    }
    const validate = values => {
        const errors = {};
        if (!values.companyName) {
            errors.companyName = "Bắt buộc"
        } else if (values.companyName.length > 150) {
            errors.companyName = "Phải dưới 150 ký tự"
        }
        if (!values.position) {
            errors.position = "Bắt buộc"
        } else if (values.position.length > 150) {
            errors.position = "Phải dưới 150 ký tự"
        }
        if (!values.startDate) {
            errors.startDate = "Bắt buộc"
        }
        if ((values.startDate > values.endDate) && values.endDate) {
            errors.startDate = "Thời gian không hợp lệ"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            companyName: '',
            position: '',
            startDate: '',
            endDate: ''
        },
        validate,
        onSubmit: async (values) => {
            const existWE = career.find((c) => {
                return c.companyName.toLowerCase().trim() === values.companyName.toLowerCase().trim()
                    && c.position.toLowerCase().trim() === values.position.toLowerCase().trim()
            })
            if (existWE) {
                enqueueSnackbar("Bạn đã có kinh nghiệm này rồi", { variant: "error" })
            } else {
                setCareer(pre => [{
                    companyName: values.companyName.trim(),
                    position: values.position.trim(),
                    startDate: values.startDate,
                    endDate: values.endDate === "" ? null : values.endDate
                }, ...pre])
                setOpen(false);
                formik.resetForm();
            }
        }
    });
    const getMinDate = () => {
        const currentDate = new Date();
        const pastDate = new Date();
        pastDate.setFullYear(currentDate.getFullYear() - 70);
        return pastDate.toISOString().slice(0, 7);
    }

    const getMaxDate = () => {
        const currentDate = new Date();
        const futureDate = new Date();
        futureDate.setFullYear(currentDate.getFullYear() + 70);
        return futureDate.toISOString().slice(0, 7)
    }
    return (
        <div>
            <IconButton onClick={handleOpen}><AddCircleOutlineIcon /></IconButton>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h5" component="h2">
                        Thêm kinh nghiệm làm việc
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Grid container px="50px" py="50px" columnSpacing={2} rowSpacing={3}>
                            <Grid item xs={3} textAlign="right">Tên nơi làm việc</Grid>
                            <Grid item xs={9}>
                                <TextField size='small' fullWidth value={formik.values.companyName}
                                    name='companyName'
                                    onChange={formik.handleChange} />
                                {
                                    formik.errors.companyName && (
                                        <FormHelperText error>
                                            {formik.errors.companyName}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Vị trí làm việc</Grid>
                            <Grid item xs={9}> <TextField size='small' fullWidth value={formik.values.position}
                                name='position'
                                onChange={formik.handleChange} />
                                {
                                    formik.errors.position && (
                                        <FormHelperText error>
                                            {formik.errors.position}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={3} textAlign="right">Thời gian làm việc</Grid>
                            <Grid item xs={9} sx={{ display: "flex", gap: 3 }}>
                                <Box>
                                    <Typography>Từ</Typography>
                                    <TextField size='small' type='month' value={formik.values.startDate}
                                        name='startDate'
                                        inputProps={{
                                            min: getMinDate(),
                                            max: new Date().toISOString().slice(0, 7)
                                        }}
                                        onChange={formik.handleChange} />
                                    {
                                        formik.errors.startDate && (
                                            <FormHelperText error>
                                                {formik.errors.startDate}
                                            </FormHelperText>
                                        )
                                    }
                                </Box>
                                <Box>
                                    <Typography>Đến <Typography variant='caption'>(Không nhập nếu vẫn đang làm việc)</Typography></Typography>
                                    <TextField size='small' type='month'
                                        value={formik.values.endDate}
                                        name='endDate'
                                        onChange={formik.handleChange}
                                        disabled={formik.values.startDate === ""}
                                        inputProps={{
                                            min: formik.values.startDate,
                                            max: getMaxDate()
                                        }}
                                    />
                                    {
                                        formik.errors.endDate && (
                                            <FormHelperText error>
                                                {formik.errors.endDate}
                                            </FormHelperText>
                                        )
                                    }
                                </Box>
                            </Grid>
                        </Grid>
                        <Box sx={{ display: "flex", justifyContent: "end", gap: 2 }}>
                            <Button variant='contained' type='submit'>Thêm</Button>
                            <Button onClick={handleClose}>Huỷ</Button>
                        </Box>
                    </form>

                </Box>
            </Modal>
        </div>
    );
}
