import WorkIcon from '@mui/icons-material/Work';
import { FormHelperText, Grid, ListItemButton, ListItemIcon, Stack, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import * as React from 'react';
import ConfirmCareer from './ConfirmCareer';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    maxHeight: "90vh",
    bgcolor: 'background.paper',
    boxShadow: 24,
    overflowY: "auto",
    p: 4,
};

export default function CareerDetail({ career, setCareer, index, currentItem }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    React.useEffect(() => {
        formik.setFieldValue("companyName", currentItem.companyName);
        formik.setFieldValue("position", currentItem.position);
        formik.setFieldValue("startDate", currentItem.startDate);
        formik.setFieldValue("endDate", currentItem.endDate);
    }, [currentItem])
    const validate = values => {
        const errors = {};
        if (!values.companyName) {
            errors.companyName = "Bắt buộc"
        }
        if (!values.position) {
            errors.position = "Bắt buộc"
        }
        if (!values.startDate) {
            errors.startDate = "Bắt buộc"
        }
        if (!values.endDate) {
            errors.endDate = "Bắt buộc"
        }
        return errors;
    };
    const formik = useFormik({
        initialValues: {
            companyName: currentItem.companyName || "",
            position: currentItem.position || '',
            startDate: currentItem.startDate || '',
            endDate: currentItem.endDate || ''
        },
        validate,
        onSubmit: async (values) => {
            const filterCar = career.filter((c, i) => i !== index);
            setCareer([...filterCar, values])
            setOpen(false);
            formik.resetForm();
        }
    });
    return (
        <div>
            <ListItemButton key={index} >
                <ListItemIcon onClick={handleOpen}>
                    <WorkIcon />
                </ListItemIcon>
                <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexGrow: 1 }} gap={2}>
                    <Typography onClick={handleOpen}>Làm {currentItem.position} tại {currentItem.companyName}</Typography>
                    <ConfirmCareer career={career} setCareer={setCareer} index={index} />
                </Stack>
            </ListItemButton>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Thêm bằng cấp hoặc chứng chỉ
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
                                    <Typography>Đến</Typography>
                                    <TextField size='small' type='month'
                                        value={formik.values.endDate}
                                        name='endDate'
                                        onChange={formik.handleChange}
                                        disabled={formik.values.startDate === ""}
                                        inputProps={{
                                            min: formik.values.startDate
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