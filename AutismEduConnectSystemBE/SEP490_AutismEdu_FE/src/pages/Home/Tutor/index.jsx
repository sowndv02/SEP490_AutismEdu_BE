import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, IconButton, Stack, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import ChipComponent from '~/components/ChipComponent'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import SubdirectoryArrowLeftIcon from '@mui/icons-material/SubdirectoryArrowLeft';
import { formatter } from '~/utils/service';
import ButtonIcon from '~/components/ButtonComponent/ButtonIcon';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import { useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';
function Tutor() {
    const [listTutor, setListTutor] = useState([]);

    const [currentTutor, setCurrentTutor] = useState(0);
    const [loading, setLoading] = useState(false);
    const nav = useNavigate();
    useEffect(() => {
        getListTutor();
    }, [])
    const getListTutor = async () => {
        try {
            setLoading(true);
            await services.TutorManagementAPI.handleGetTutors((res) => {
                setListTutor(res.result.slice(0, 6))
            }, (err) => {
                console.log(err);
            }, {
            })
            setLoading(false);
        } catch (error) {
            console.log(error);
        }
    };
    const handleClickToProfile = (id) => {
        nav(`/autismedu/tutor-profile/${id}`);
    };

    const formatAddress = (address = '') => {
        const splitedAdd = address?.split("|");
        let formatedAddress = "";
        if (address) {
            splitedAdd.forEach((s, index) => {
                if (index === 0)
                    formatedAddress = s + formatedAddress;
                else formatedAddress = s + " - " + formatedAddress;
            });
        }
        return formatedAddress;
    };
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center', bgcolor: "#f9f9ff" }}>

            <ChipComponent text="GIA SƯ NỔI BẬT" bgColor="#e4e9fd" color="#2f57f0" />
            <Box textAlign={'center'} sx={{ marginBottom: "50px" }}>
                <Typography variant='h2' sx={{ width: "60%", margin: "auto", color: "#192335", }}>
                    Tìm Gia Sư Phù Hợp Nhất Cho Con Của Bạn
                </Typography>
            </Box>
            <Stack direction='row' sx={{ justifyContent: "center", width: "100vw" }}>
                <Stack direction="row" sx={{
                    textAlign: "left",
                    width: {
                        xl: "80%",
                        lg: "90%"
                    },
                    gap: "20px"
                }}>
                    {
                        listTutor.length !== 0 && (
                            <>
                                <Box sx={{ width: "60%" }} pt={1}>
                                    <Card sx={{ display: 'flex', p: "30px", width: "100%", boxSizing: "border-box", alignItems: "center" }}>
                                        <Box sx={{ flexBasis: "40%" }}>
                                            <img src={listTutor[currentTutor].imageUrl}
                                                style={{ objectFit: "cover", width: "100%", height: "auto" }}
                                                alt='Live from space album cover'
                                            />
                                        </Box>
                                        <Box sx={{ display: 'flex', flexDirection: 'column', flexBasis: "60%" }}>
                                            <CardContent sx={{ flex: '1 0 auto' }}>
                                                <Typography component="div" variant="h4">
                                                    {listTutor[currentTutor].fullName}
                                                </Typography>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                    <LocationOnOutlinedIcon />
                                                    <Typography>{formatAddress(listTutor[currentTutor].address)}</Typography>
                                                </Box>
                                                <Box sx={{ display: "flex", gap: "20px", flexWrap: "wrap", mt: 2 }}>
                                                    <Box sx={{
                                                        display: "flex", alignItems: "center", gap: "10px",
                                                        '&:hover': {
                                                            color: "blue"
                                                        }
                                                    }}>
                                                        <LocalPhoneOutlinedIcon />
                                                        <a href={`tel:${listTutor[currentTutor].phoneNumber}`}><Typography sx={{
                                                            '&:hover': {
                                                                color: "blue"
                                                            }
                                                        }}>{listTutor[currentTutor].phoneNumber}</Typography></a>
                                                    </Box>
                                                    <Box sx={{
                                                        display: "flex", alignItems: "center", gap: "10px",
                                                        '&:hover': {
                                                            color: "blue"
                                                        }
                                                    }}>
                                                        <EmailOutlinedIcon />
                                                        <a href={`mailto:${listTutor[currentTutor].email}`}><Typography
                                                            sx={{
                                                                '&:hover': {
                                                                    color: "blue"
                                                                }
                                                            }}
                                                        >{listTutor[currentTutor].email}</Typography></a>
                                                    </Box>
                                                </Box>
                                                <Typography mt={4} variant='h5' color={'green'}>{formatter.format(listTutor[currentTutor].priceFrom)} - {formatter.format(listTutor[currentTutor].priceEnd)}<Typography component="span" variant='subtitle1' color={'gray'}> / buổi <small>({listTutor[currentTutor]?.sessionHours} tiếng)</small></Typography></Typography>

                                            </CardContent>
                                            <CardActions>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />} onClick={() => handleClickToProfile(listTutor[currentTutor]?.userId)}>Tìm hiểu thêm</Button>
                                            </CardActions>
                                        </Box>

                                    </Card>
                                </Box>
                                <Box sx={{ width: "40%" }} >
                                    <Grid container m={0} spacing={{ xs: 2, md: 3 }} textAlign={"left"} sx={{ height: "100%" }}
                                        columnSpacing={2} rowSpacing={1}
                                    >
                                        {
                                            listTutor.map((l, index) => {
                                                return (
                                                    <Grid item xs={12} md={4} sx={{ height: "50%" }} key={l.id}>
                                                        <Card sx={{ width: "100%", height: "100%", p: 1, position: 'relative', cursor: "pointer" }}>
                                                            <CardMedia
                                                                component="img"
                                                                height="100%"
                                                                image={l.imageUrl}
                                                                alt="green iguana"
                                                                onClick={() => setCurrentTutor(index)}
                                                            />
                                                            {
                                                                currentTutor === index && (
                                                                    <Box
                                                                        sx={{
                                                                            position: 'absolute',
                                                                            top: 0,
                                                                            left: 0,
                                                                            right: 0,
                                                                            bottom: 0,
                                                                            bgcolor: '#0009c933',
                                                                            display: 'flex',
                                                                            justifyContent: 'center',
                                                                            alignItems: 'center',
                                                                            transition: 'opacity 0.3s ease',
                                                                        }}
                                                                    >
                                                                        <SubdirectoryArrowLeftIcon sx={{ color: 'white', fontSize: 40 }} />
                                                                    </Box>
                                                                )
                                                            }
                                                        </Card>
                                                    </Grid>
                                                )
                                            })
                                        }
                                    </Grid>
                                </Box>
                            </>
                        )
                    }
                </Stack>
            </Stack >
            <Box mt={5} textAlign="center">
                <ButtonIcon
                    action={() => {
                        nav(PAGES.ROOT + PAGES.LISTTUTOR);
                    }}
                    text={"XEM THÊM GiA SƯ"} width="400px" height="70px" fontSize="20px" />
            </Box>
            <LoadingComponent open={loading} />
        </Box >
    )
}
export default Tutor
