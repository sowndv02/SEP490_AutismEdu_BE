import DeleteIcon from '@mui/icons-material/Delete';
import { Box, Button, FormControl, FormHelperText, Grid, IconButton, InputLabel, MenuItem, Paper, Select, Stack, TextField, Typography } from '@mui/material';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import React, { useRef, useState } from 'react';
import ConfirmDialog from '~/components/ConfirmDialog';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
function AssessmentCreation() {
    const pointArr = [1, 1.5, 2, 2.5, 3, 3.5, 4]
    const [point, setPoint] = useState(1);
    const [listAss, setListAss] = useState([]);
    const [assessmentName, setAssessmentName] = useState("");
    const [selectedPoint, setSelectedPoint] = useState([]);
    const [loading, setLoading] = useState(false);
    const [openConfirm, setOpenConfirm] = useState(false);
    const validateDetail = (values) => {
        const errors = {};
        if (!values.assessmentDetail) {
            errors.assessmentDetail = "Bắt buộc nhập";
        }
        else if (values.assessmentDetail.trim().length < 20) {
            errors.assessmentDetail = "Đánh giá phải trên 20 ký tự!";
        }
        if (values.assessmentDetail.trim().length > 250) {
            errors.assessmentDetail = "Đánh giá phải dưới 250 ký tự!";
        }
        return errors
    }
    const optionFormik = useFormik({
        initialValues: {
            assessmentDetail: ""
        }, validate: validateDetail,
        onSubmit: (values) => {
            handleAddPoint();
        }
    })
    const handleAddPoint = () => {
        const newAss = {
            point: point,
            optionText: optionFormik.values.assessmentDetail.trim()
        }
        const sortedList = [newAss, ...listAss].sort((firstItem, secondItem) => {
            return firstItem.point - secondItem.point
        })
        setListAss(sortedList)
        setSelectedPoint([...selectedPoint, point]);
        for (let i = 0; i < pointArr.length; i++) {
            if (![...selectedPoint, point].includes(pointArr[i])) {
                setPoint(pointArr[i]);
                break;
            }
        }
        optionFormik.resetForm()
    }

    const handleDelete = (index) => {
        const deletedItem = listAss.find((l, i) => {
            return i === index
        });
        const filterPoint = selectedPoint.filter((s, i) => {
            return s !== deletedItem.point
        })
        const deletedList = listAss.filter((l, i) => {
            return i !== index;
        })
        setListAss(deletedList);
        setSelectedPoint(filterPoint);
        if (selectedPoint.length === 7) {
            setPoint(deletedItem.point);
        }
        if (deletedItem.point < point && selectedPoint.length < 7) {
            setPoint(deletedItem.point)
        }
    }

    const handleSubmit = async () => {
        if (assessmentName.trim() === "") {
            enqueueSnackbar("Bạn chưa nhập tên đánh giá", { variant: "error" })
            return;
        }
        else if (assessmentName.trim().length > 150) {
            enqueueSnackbar("Tiêu đề phải dưới 150 ký tự", { variant: "error" })
            return;
        }
        else if (listAss.length < 7) {
            enqueueSnackbar("Bạn chưa nhập đủ đánh giá", { variant: "error" })
            return;
        }
        try {
            setLoading(true);
            await services.AssessmentManagementAPI.createAssessment({
                question: assessmentName.trim(),
                assessmentOptions: listAss
            }, (res) => {
                setListAss([]);
                setSelectedPoint([]);
                setAssessmentName("");
                enqueueSnackbar("Tạo đánh giá thành công!", { variant: "success" })
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Paper variant="elevation" sx={{
            py: 3,
            px: 5
        }}>
            <Typography variant='h4'>Thêm đánh giá</Typography>
            <Box px="100px">
                <Grid container mt={3} rowSpacing={4} m="auto">
                    <Grid item xs={2}><Typography variant='h6'>Tên đánh giá</Typography></Grid>
                    <Grid item xs={10}>
                        <TextField
                            size='small'
                            sx={{ width: "70%" }}
                            value={assessmentName}
                            onInput={(e) => { setAssessmentName(e.target.value.toUpperCase()) }}
                            placeholder='Nhập tên đánh giá vào đây'
                        />
                        <Typography textAlign="right" sx={{ width: "70%" }}>{assessmentName?.length} / 150</Typography>
                    </Grid>
                    <Grid item xs={2}><Typography variant='h6'>Chi tiết đánh giá</Typography></Grid>
                    <Grid item xs={10}></Grid>
                    {
                        selectedPoint.length < 7 && (
                            <>

                                <Grid item xs={2}>
                                    <FormControl sx={{ width: "80%" }}>
                                        <InputLabel id="label-point">Điểm</InputLabel>
                                        <Select
                                            labelId="label-point"
                                            value={point}
                                            label="Point"
                                            onChange={(e) => { setPoint(e.target.value) }}
                                        >
                                            {
                                                pointArr.map((p) => {
                                                    return (
                                                        <MenuItem value={p} disabled={selectedPoint.includes(p)} key={p}>{p} điểm</MenuItem>
                                                    )
                                                })
                                            }
                                        </Select>
                                    </FormControl>
                                </Grid>
                                <Grid item xs={10}>
                                    <form onSubmit={optionFormik.handleSubmit}>
                                        <TextField
                                            size='small'
                                            multiline
                                            rows={4}
                                            sx={{ width: "70%" }}
                                            label="Nội dung"
                                            variant='outlined'
                                            name='assessmentDetail'
                                            value={optionFormik.values.assessmentDetail}
                                            onChange={optionFormik.handleChange}
                                        />
                                        {
                                            <Stack direction='row' justifyContent='space-between' sx={{ width: "70%" }}>
                                                <Box>
                                                    {
                                                        optionFormik.errors && (
                                                            <FormHelperText error>
                                                                {optionFormik.errors.assessmentDetail}
                                                            </FormHelperText>
                                                        )
                                                    }
                                                </Box>
                                                <Typography> {optionFormik.values.assessmentDetail.length} / 250</Typography>
                                            </Stack>
                                        }
                                        <Box mt={3}>
                                            <Button variant='contained' type='submit'>Thêm</Button>
                                        </Box>
                                    </form>
                                </Grid>
                            </>
                        )
                    }
                    {
                        listAss.length !== 0 && listAss.map((l, index) => {
                            return (
                                <React.Fragment key={index}>
                                    <Grid item xs={2}>
                                        <Typography>{l.point} điểm</Typography>
                                    </Grid>
                                    <Grid item xs={10}>
                                        <Stack direction='row' alignItems="center">
                                            <Typography sx={{ width: "60%" }}>{l.optionText}</Typography>
                                            <IconButton onClick={() => { handleDelete(index) }}
                                                sx={{ color: '#ff3e1d' }}
                                            ><DeleteIcon /></IconButton>
                                        </Stack>
                                    </Grid>
                                </React.Fragment>
                            )
                        })
                    }
                    <Button variant='contained' sx={{ mt: 5, mb: 3 }} onClick={() => setOpenConfirm(true)}>Tạo đánh giá</Button>
                </Grid>
            </Box >
            <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm}
                title={"Tạo đánh giá"}
                content={"Kiểm tra thật kĩ tên của đánh giá, vì tên đánh giá không thể cập nhật!"}
                handleAction={handleSubmit}
            />
            <LoadingComponent open={loading} />
        </Paper >
    )
}

export default AssessmentCreation
