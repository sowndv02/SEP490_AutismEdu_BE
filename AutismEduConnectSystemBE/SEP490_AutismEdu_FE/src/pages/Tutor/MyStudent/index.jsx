import { Avatar, Box, Button, Card, CardActions, CardContent, Chip, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useDispatch } from 'react-redux';
import { Link } from 'react-router-dom';
import services from '~/plugins/services';
import { setListStudent } from '~/redux/features/listStudent';

function MyStudent() {
    const [status, setStatus] = useState(1);
    const dispatch = useDispatch();
    const [listStudents, setListStudents] = useState([]);
    const [currentPage, setCurrentPage] = useState(1);
    const [total, setTotal] = useState(0);
    useEffect(() => {
        getListStudent();
    }, [])

    useEffect(() => {
        getListStudent();
    }, [currentPage])

    useEffect(() => {
        if (currentPage === 1) {
            getListStudent();
        } else {
            setCurrentPage(1);
        }
    }, [status])

    const getListStudent = async () => {
        try {
            let statusText = '';
            if (status === 1) {
                statusText = 'Teaching'
            } else if (status === 0) {
                statusText = 'Stop'
            }
            await services.StudentProfileAPI.getListStudent((res) => {
                setTotal(res.pagination.total)
                if (currentPage === 1) {
                    setListStudents(res.result);
                } else {
                    setListStudents([...listStudents, ...res.result]);
                }
                if (status === 1 && currentPage === 1) {
                    dispatch(setListStudent(res.result))
                }
            }, (error) => {
                console.log(error);
            }, {
                status: statusText,
                pageNumber: currentPage
            })
        } catch (error) {
            console.log(error);
        }
    }

    const formatDate = (d) => {
        const date = new Date(d);
        return `${date.getDate()} - ${date.getMonth() + 1} - ${date.getFullYear()}`
    }

    const formatAddress = (d) => {
        try {
            const splitedAdd = d.split("|");
            return `${splitedAdd[3]} - ${splitedAdd[2]} - ${splitedAdd[1]} - ${splitedAdd[0]}`
        } catch (error) {
            return "";
        }

    }

    return (
        <Box sx={{
            px: "50px",
            py: '30px'
        }}>
            <Box mb={3}>
                <Chip label="Đang học" variant={status === 1 ? "filled" : "outlined"}
                    onClick={() => setStatus(1)}
                    sx={{ cursor: "pointer", mr: 2 }}
                />
                <Chip label="Đã hoàn thành" variant={status === 0 ? "filled" : "outlined"}
                    onClick={() => setStatus(0)}
                    sx={{ cursor: "pointer" }} />
            </Box>
            <Stack direction="row" sx={{
                gap: 5,
                flexWrap: 'wrap'
            }}>
                {
                    listStudents && listStudents.length === 0 &&
                    <Typography sx={{ textAlign: "center" }}>Bạn đang không dạy học sinh nào</Typography>
                }
                {
                    listStudents && listStudents.length !== 0 && listStudents.map((l) => {
                        return (
                            <Card sx={{ width: 345 }} key={l.id}>
                                <Box sx={{
                                    background: "linear-gradient(to right, #4468f1, #c079ea);",
                                    position: "relative",
                                    p: "20px"
                                }}>
                                    <Typography gutterBottom variant="h5" component="div" color={"white"}>
                                        {l.studentCode}
                                    </Typography>
                                    <Typography gutterBottom component="div" sx={{ width: "70%" }} color={"white"}>
                                        {l.name}
                                    </Typography>
                                    <Avatar alt={l.studentCode} src={l?.imageUrlPath ? l.imageUrlPath : "/"} sx={{
                                        height: "80px",
                                        width: "80px",
                                        fontSize: "40px",
                                        bgcolor: "#c2185b",
                                        position: "absolute",
                                        bottom: "-40px",
                                        right: "20px"
                                    }} />
                                </Box>
                                <CardContent>
                                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                                        Ngày sinh: {formatDate(l.birthDate)}
                                    </Typography>
                                    <Typography variant="body2" sx={{ color: 'text.secondary', width: "70%" }} mt={2}>
                                        Địa chỉ: {formatAddress(l.address)}
                                    </Typography>
                                </CardContent>
                                <CardActions>
                                    <Link to={'/autismtutor/student-detail/' + l.id}>
                                        <Button size="small">Xem chi tiết</Button>
                                    </Link>
                                </CardActions>
                            </Card>
                        )
                    })
                }
            </Stack>
            <Box sx={{ textAlign: "center", mt: 2 }}>
                {
                    (currentPage * 10 < total) && (
                        <Button onClick={() => setCurrentPage(currentPage + 1)}
                            variant='contained'
                            color='success'
                        >Xem thêm
                        </Button>
                    )
                }
            </Box>
        </Box>
    )
}

export default MyStudent
