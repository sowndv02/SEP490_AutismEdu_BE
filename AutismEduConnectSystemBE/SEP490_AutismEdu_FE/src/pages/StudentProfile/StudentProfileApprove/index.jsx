import { Avatar, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, Divider, Grid, Modal, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react'
import { useSelector } from 'react-redux';
import { useNavigate, useParams } from 'react-router-dom'
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { userInfor } from '~/redux/features/userSlice';
import PAGES from '~/utils/pages';

const days = [
    {
        id: 1,
        day: "Thứ 2"
    },
    {
        id: 2,
        day: "Thứ 3"
    },
    {
        id: 3,
        day: "Thứ 4"
    },
    {
        id: 4,
        day: "Thứ 5"
    },
    {
        id: 5,
        day: "Thứ 6"
    },
    {
        id: 6,
        day: "Thứ 7"
    },
    {
        id: 0,
        day: "Chủ nhật"
    }
];
function StudentProfileApprove() {
    const { id } = useParams();
    const [studentProfile, setStudentProfile] = useState(null);
    const [open, setOpen] = React.useState(false);
    const [action, setAction] = useState(1);
    const [loading, setLoading] = useState(false);
    const parentInfor = useSelector(userInfor);
    const nav = useNavigate();
    const handleClickOpen = (e) => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    useEffect(() => {
        handleGetStudentProfile();
    }, []);

    const formatAddress = (address) => {
        const addressArr = address.split("|");
        return `${addressArr[3]} - ${addressArr[2]} - ${addressArr[1]} - ${addressArr[0]}`
    }
    const handleGetStudentProfile = async () => {
        try {
            await services.StudentProfileAPI.getStudentProfileById(id, (res) => {
                console.log(res);
                setStudentProfile(res.result)
                console.log(res.result);
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }
    const formatDate = (date) => {
        const birthday = new Date(date);
        return `${birthday.getDate()} / ${birthday.getMonth()} / ${birthday.getFullYear()}`
    }

    const getExpiredDate = (createdAt) => {
        const date = new Date(createdAt);
        date.setDate(date.getDate() + 1);
        return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}   ${date.getHours()}:${date.getMinutes()}`
    }
    const handleSubmit = async () => {
        try {
            services.StudentProfileAPI.changeStudentProfileStatus({
                id: studentProfile.id,
                statusChange: action
            }, (res) => {
                console.log(res);
                if (action === 1) {
                    setStudentProfile({
                        ...studentProfile,
                        status: 1
                    })
                } else {
                    setStudentProfile({
                        ...studentProfile,
                        status: 2
                    })
                }
                handleClose();
            }, (err) => {
                console.log(err);
            })
        } catch (error) {
            console.log(error);
        }
    }

    console.log(studentProfile);
    return (
        <Stack direction="row" p="20px" sx={{
            bgcolor: "#f8fafb", width: '100%',
            pb: 3,
            justifyContent: "center"
        }} overflow="auto">
            {
                studentProfile && (
                    <Box sx={{ width: "80%" }}>
                        <Box sx={{ bgcolor: "white", p: 2, display: "flex", justifyContent: "space-between" }}>
                            <Box>
                                <Typography variant='h4'>Thông tin học sinh</Typography>
                                {
                                    studentProfile?.status === 3 &&
                                    <Typography mt={1} color="red">Ngày hết hạn: {getExpiredDate(studentProfile.createdDate)}</Typography>
                                }
                            </Box>
                            <Box>
                                {
                                    studentProfile?.status === 1 && <Typography padding={1} sx={{ border: "1px solid green", mb: 2, textAlign: "center", color: "green" }}>Đã Chấp nhận</Typography>
                                }
                                {
                                    studentProfile?.status === 2 && <Typography padding={1} sx={{ border: "1px solid red", mb: 2, textAlign: "center", color: "red" }}>Đã Từ chối</Typography>
                                }
                                {
                                    studentProfile?.status === 3 && (
                                        <>
                                            <Typography padding={1} sx={{ border: "1px solid blue", mb: 2, textAlign: "center", color: "blue" }}>Đang chờ</Typography>
                                            <Button sx={{ mr: 1 }} variant='contained' onClick={() => { handleClickOpen(); setAction(1) }}>Chấp nhận</Button>
                                            <Button variant='contained' sx={{
                                                bgcolor: "#fa3d3d",
                                                ":hover": {
                                                    bgcolor: "red"
                                                }
                                            }}
                                                onClick={() => { handleClickOpen(); setAction(2) }}
                                            >Từ chối</Button>
                                        </>
                                    )
                                }
                            </Box>
                        </Box>
                        <Stack direction='row' gap={2} mt={3}>
                            <Box sx={{ width: "40%" }}>
                                <Card sx={{ px: 2 }}>
                                    <CardContent sx={{ px: 0 }}>
                                        <Typography variant='h5'>Thông tin gia sư</Typography>
                                    </CardContent>
                                    <Avatar src={studentProfile?.tutor?.imageUrl} alt='Nguyen' sx={{
                                        margin: "auto",
                                        width: "150px",
                                        height: "150px"
                                    }} />
                                    <CardContent sx={{ px: 0 }}>
                                        <Grid container rowSpacing={2} mt={1}>
                                            <Grid item xs={4}>
                                                <Typography>Họ tên:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{studentProfile.tutor.fullName}</Typography>
                                            </Grid>
                                            <Grid item xs={4}>
                                                <Typography>Số điện thoại:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{studentProfile.tutor.phoneNumber}</Typography>
                                            </Grid>
                                            <Grid item xs={4}>
                                                <Typography>Địa chỉ:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{formatAddress(studentProfile.tutor.address)}</Typography>
                                            </Grid>
                                        </Grid>
                                    </CardContent>
                                </Card>
                                <Card sx={{ mt: 3, px: 2 }}>
                                    <CardContent sx={{ px: 0 }}>
                                        <Typography variant='h5'>Thông tin trẻ</Typography>
                                    </CardContent>
                                    <Avatar src={studentProfile.imageUrlPath} alt='Nguyen' sx={{
                                        margin: "auto",
                                        width: "150px",
                                        height: "150px"
                                    }} />
                                    <CardContent sx={{ p: 0 }}>
                                        <Grid container rowSpacing={2} mt={1}>
                                            <Grid item xs={4}>
                                                <Typography>Họ tên trẻ:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{studentProfile.name}</Typography>
                                            </Grid>
                                            <Grid item xs={4}>
                                                <Typography>Ngày sinh:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{formatDate(studentProfile.birthDate)}</Typography>
                                            </Grid>
                                            <Grid item xs={4}>
                                                <Typography>Giới tính:</Typography>
                                            </Grid>
                                            <Grid item xs={8}>
                                                <Typography>{studentProfile.isMale ? "Nam" : "Nữ"}</Typography>
                                            </Grid>
                                        </Grid>
                                    </CardContent>
                                </Card>
                                <Card sx={{ px: 2, mt: 3 }}>
                                    <CardContent sx={{ px: 0 }}>
                                        <Typography variant='h5'>Lịch học</Typography>

                                        <Box sx={{ display: "flex", mt: 3, flexWrap: "wrap", gap: 3 }} >
                                            {
                                                studentProfile && studentProfile.scheduleTimeSlots.map((s) => {
                                                    return (
                                                        <Box sx={{
                                                            display: "flex",
                                                            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
                                                            p: 2,
                                                            gap: 2, alignItems: "center"
                                                        }} key={s.id}>
                                                            <Typography sx={{ fontSize: "12px" }}>{days.find((day) => day.id === s.weekday).day}</Typography>
                                                            <Divider orientation='vertical' sx={{ bgcolor: "black" }} />
                                                            <Typography sx={{ fontSize: "12px" }}>{s.from} - {s.to}</Typography>
                                                        </Box>
                                                    )
                                                })
                                            }
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Box>
                            <Box sx={{ width: "60%" }}>
                                <Card sx={{ px: 2 }}>
                                    <CardContent sx={{ px: 0 }}>
                                        <Typography variant='h5'>Tình trạng ban đầu</Typography>
                                        <Typography sx={{ whiteSpace: "break-spaces", mt: 3 }}>{studentProfile.initialAssessmentResults?.condition}</Typography>
                                    </CardContent>
                                </Card>
                                <Card sx={{ px: 2, mt: 3 }}>
                                    <CardContent sx={{ px: 0 }}>
                                        <Typography variant='h5' mt={2}>Bảng đánh giá</Typography>
                                        <Table sx={{ minWidth: 650 }} aria-label="simple table">
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell >Vấn đề</TableCell>
                                                    <TableCell >Đánh giá</TableCell>
                                                    <TableCell >Điểm</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {studentProfile.initialAssessmentResults?.assessmentResults.map((assessment) => (
                                                    <TableRow
                                                        key={assessment.id}
                                                        sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                                                    >
                                                        <TableCell >{assessment.question}</TableCell>
                                                        <TableCell >{assessment.optionText}</TableCell>
                                                        <TableCell >{assessment.point}</TableCell>
                                                    </TableRow>
                                                ))}
                                            </TableBody>
                                        </Table>
                                    </CardContent>
                                </Card>
                            </Box>
                        </Stack>
                    </Box>
                )
            }

            <Dialog
                open={open}
                onClose={handleClose}
                sx={{ height: "500px" }}
            >
                <DialogContent>
                    <DialogContentText>
                        Bạn có muốn {action === 1 ? "chấp nhận" : "từ chối"} đơn đăng ký này?
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleClose}>Huỷ bỏ</Button>
                    <Button onClick={handleSubmit}>{action === 1 ? "Chấp nhận" : "Từ chối"}</Button>
                </DialogActions>
                <LoadingComponent open={loading} setLoading={setLoading} />
            </Dialog>
        </Stack>
    )
}

export default StudentProfileApprove
