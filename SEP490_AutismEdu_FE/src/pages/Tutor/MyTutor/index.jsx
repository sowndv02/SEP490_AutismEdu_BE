import LocalPhoneIcon from '@mui/icons-material/LocalPhone';
import { Button, Chip, Stack } from '@mui/material';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardMedia from '@mui/material/CardMedia';
import Typography from '@mui/material/Typography';
import * as React from 'react';
import { useNavigate } from 'react-router-dom';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
function MyTutor() {
    const [status, setStatus] = React.useState(1);
    const [listTutor, setListTutor] = React.useState([]);
    const [currentPage, setCurrentPage] = React.useState(1);
    const [total, setTotal] = React.useState(0);
    const nav = useNavigate();
    React.useEffect(() => {
        if (currentPage === 1) {
            getStudentProfile();
        } else {
            setCurrentPage(1);
        }
    }, [status])
    React.useEffect(() => {
        getStudentProfile();
    }, [currentPage])
    const getStudentProfile = async () => {
        let apiStatus = "Teaching";
        if (status === 1) {
            apiStatus = "Teaching";
        } else if (status === 2) {
            apiStatus = "Stop"
        }
        try {
            await services.StudentProfileAPI.getMyTutor((res) => {
                setTotal(res.pagination.total)
                if (currentPage === 1) {
                    setListTutor(res.result);
                } else {
                    setListTutor([...listTutor, ...res.result]);
                }
            }, (err) => {
                console.log(err);
            }, {
                status: apiStatus,
                pageNumber: currentPage
            })
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Stack direction='row' justifyContent="center" pt={3}>
            <Box sx={{ width: "80%", minHeight: "80vh" }}>
                <Typography sx={{}} variant='h4'>Gia sư của tôi</Typography>
                <Box mt={3}>
                    <Chip label="Đang học" variant={status === 1 ? "filled" : "outlined"}
                        onClick={() => setStatus(1)}
                        sx={{ cursor: "pointer", mr: 2 }}
                    />
                    <Chip label="Đã hoàn thành" variant={status === 2 ? "filled" : "outlined"}
                        onClick={() => setStatus(2)}
                        sx={{ cursor: "pointer" }} />
                </Box>
                <Stack direction='row' mt={5} gap={5} flexWrap='wrap'>
                    {
                        listTutor && listTutor.length !== 0 && listTutor.map((l) => {
                            return (
                                <Card sx={{
                                    display: 'flex',
                                    transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                    '&:hover': {
                                        transform: "scale(1.05) translateY(-10px)",
                                        boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                    }
                                }} key={l.id}>
                                    <Box sx={{ display: 'flex', flexDirection: 'column', width: "250px" }}>
                                        <CardContent sx={{ flex: '1 0 auto' }}>
                                            <Typography component="div" variant="h5">
                                                {l.tutorName}
                                            </Typography>
                                            <Typography
                                                component="div"
                                                sx={{ fontSize: "14px" }}
                                            >
                                                <LocalPhoneIcon sx={{ fontSize: "14px" }} /> {l.tutorPhoneNumber}
                                            </Typography>
                                            <Typography mt={5}><b fontWeight='bold'>Tên trẻ: </b>{l.childName}</Typography>
                                            <Typography color={status === 1 ? "green" : "red"}>
                                                ({status === 1 ? "Đang học" : "Kết thúc"})
                                            </Typography>
                                            <Button sx={{ mt: 2 }} onClick={() => nav(PAGES.ROOT + PAGES.MY_TUTOR + "/" + l.id)}>Xem thêm</Button>
                                        </CardContent>
                                    </Box>
                                    <CardMedia
                                        component="img"
                                        sx={{ width: 151 }}
                                        image={l.tutorImageUrl}
                                        alt={l.tutorName}
                                    />
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
        </Stack>

    )
}

export default MyTutor
