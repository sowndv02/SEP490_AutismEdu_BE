import React, { useEffect, useState } from 'react';
import { Grid, Box, TextField, InputAdornment, Card, CardContent, CardMedia, Typography, Rating, Button, Link, IconButton, CardActions, Container, Select, MenuItem, InputLabel, FormControl, CircularProgress } from '@mui/material';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Stack from '@mui/material/Stack';
import GridViewIcon from '@mui/icons-material/GridView';
import FormatListBulletedIcon from '@mui/icons-material/FormatListBulleted';
import SearchIcon from '@mui/icons-material/Search';
import FilterListIcon from '@mui/icons-material/FilterList';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import ClassOutlinedIcon from '@mui/icons-material/ClassOutlined';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import Person3OutlinedIcon from '@mui/icons-material/Person3Outlined';
import LocalPhoneIcon from '@mui/icons-material/LocalPhone';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import ButtonComponent from '~/Components/ButtonComponent';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import StarIcon from '@mui/icons-material/Star';
import CALL_API_ADDRESS from '~/utils/call_api_address';
import axios from 'axios';
import FormSearch from './FormSearch/FormSearch';

function SearchTutor() {
    const [searchCriteria, setSearchCriteria] = useState({
        searchValue: "",
        address: "",
        selectedRating: ""
    });
    const [selected, setSelected] = useState('grid');
    const [searchValue, setSearchValue] = useState('');
    const arrCenter = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    const [visibleCards, setVisibleCards] = useState(6);
    const [showFilters, setShowFilters] = useState(false);
    
    const handleFilterClick = () => {
        setShowFilters(!showFilters);
    };

    const handleSearch = () => {
        if (searchValue.trim()) {
            console.log('Searching for:', searchValue);

        } else {
            console.log('Please enter a search term');
        }
    };

    const handleButtonClick = (button) => {
        setSelected(button);
    };
    function handleClick(event) {
        event.preventDefault();
        console.info('You clicked a breadcrumb.');
    };
    const handleShowMore = () => {
        setVisibleCards((prevVisible) => prevVisible + 6);
    };

    // Khi người dùng chọn đánh giá
    const handleRatingChange = (event) => {
        setSelectedRating(event.target.value);
    };

    const breadcrumbs = [
        <Link underline="hover" key="1" color="inherit" href="/" onClick={handleClick}>
            Trang chủ
        </Link>, ,
        <Typography key="3" sx={{ color: 'rgb(107, 115, 133)' }}>
            Danh sách trung tâm
        </Typography>,
    ];

    const formatter = new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
    });

    return (
        <Box sx={{ height: 'auto' }}>
            <FormSearch selected={selected} setSelected={setSelected}
                showFilters={showFilters} handleSearch={handleSearch} handleFilterClick={handleFilterClick} searchCriteria={searchCriteria} setSearchCriteria={setSearchCriteria} />
            <Grid container sx={{ height: 'auto', position: "relative", top: '-170px' }}>

                <Grid item xs={2} />

                <Grid item xs={8} sx={{ height: 'auto' }} mt={5}>
                    <Grid container sx={{ height: 'auto' }}>
                        <Grid item>
                            <Grid container spacing={5}>
                                {selected === "list" ?
                                    arrCenter.slice(0, visibleCards).map((c, index) =>
                                    (<Grid item xs={4}>
                                        <Card sx={{
                                            padding: "20px", minHeight: "auto",
                                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                            '&:hover': {
                                                transform: "scale(1.05) translateY(-10px)",
                                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                            },
                                            borderRadius: "5px"
                                        }}>
                                            <CardMedia
                                                sx={{ height: 240, objectPosition: "top", objectFit: "cover" }}
                                                image="https://www.workitdaily.com/media-library/a-happy-young-teacher-grades-an-assignment-from-one-of-her-students.jpg?id=22146397&width=1200&height=800&quality=85&coordinates=0%2C0%2C0%2C1"
                                                title="apeople"
                                            />
                                            <CardContent>
                                                <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                                    <Box sx={{ display: "flex", alignItems: "center" }}>
                                                        <Rating name="read-only" value={3} readOnly />
                                                        <Typography ml={2}>(20 reviews)</Typography>
                                                    </Box>
                                                    <IconButton>
                                                        <BookmarkBorderIcon />
                                                    </IconButton>
                                                </Box>
                                                <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>
                                                    Nguyễn Thuỳ Trang
                                                </Typography>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                    <LocationOnOutlinedIcon />
                                                    <Typography>Hồ Chí Minh</Typography>
                                                </Box>
                                                <Typography mt={2} sx={{
                                                    display: '-webkit-box',
                                                    WebkitLineClamp: 3,
                                                    WebkitBoxOrient: 'vertical',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                }}>
                                                    HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho
                                                </Typography>

                                                <Box sx={{ display: "flex", gap: "15px" }}>
                                                    <Box sx={{
                                                        display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                        '&:hover': {
                                                            color: "blue"
                                                        }
                                                    }}>
                                                        <LocalPhoneOutlinedIcon />
                                                        <a href='tel:0325845628'><Typography sx={{
                                                            '&:hover': {
                                                                color: "blue"
                                                            }
                                                        }}>0325845628</Typography></a>
                                                    </Box>
                                                    <Box sx={{
                                                        display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                        '&:hover': {
                                                            color: "blue"
                                                        }
                                                    }}>
                                                        <EmailOutlinedIcon />
                                                        <a href='mailto:xuanthulab.net@gmail.com'>
                                                            <Typography
                                                                sx={{
                                                                    whiteSpace: 'nowrap',
                                                                    overflow: 'hidden',
                                                                    textOverflow: 'ellipsis',
                                                                    maxWidth: '180px',
                                                                    '&:hover': {
                                                                        color: "blue"
                                                                    }
                                                                }}
                                                            >
                                                                nguyenthuytrang@gmail.com
                                                            </Typography>
                                                        </a>
                                                    </Box>

                                                </Box>
                                                <Typography mt={4} variant='h5'>{formatter.format(100000)} / buổi</Typography>
                                            </CardContent>
                                            <CardActions>
                                                <Button sx={{ fontSize: "20px" }}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                            </CardActions>
                                        </Card>
                                    </Grid>)
                                    ) : arrCenter.slice(0, visibleCards).map((c, index) => (<Grid item xs={12}>
                                        <Card sx={{
                                            display: 'flex',
                                            padding: "20px",
                                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                            '&:hover': {
                                                transform: "scale(1.05) translateY(-10px)",
                                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                            },
                                            borderRadius: "5px",
                                            minHeight: "auto"
                                        }}>

                                            <CardMedia
                                                sx={{ width: '40%', height: 'auto', borderRadius: '8px' }}
                                                image="https://www.workitdaily.com/media-library/a-happy-young-teacher-grades-an-assignment-from-one-of-her-students.jpg?id=22146397&width=1200&height=800&quality=85&coordinates=0%2C0%2C0%2C1"
                                                title="Hanoi"
                                            />


                                            <Box sx={{ display: 'flex', flexDirection: 'column', width: '60%', padding: "20px" }}>
                                                <CardContent sx={{ flexGrow: 1 }}>
                                                    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                                            <Rating name="read-only" value={3} readOnly />
                                                            <Typography ml={2}>(20 reviews)</Typography>
                                                        </Box>
                                                        <IconButton>
                                                            <BookmarkBorderIcon />
                                                        </IconButton>
                                                    </Box>
                                                    <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>
                                                        Nguyễn Thuỳ Trang
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                        <LocationOnOutlinedIcon />
                                                        <Typography>Hồ Chí Minh</Typography>
                                                    </Box>
                                                    <Typography mt={2} sx={{
                                                        display: '-webkit-box',
                                                        WebkitLineClamp: 3,
                                                        WebkitBoxOrient: 'vertical',
                                                        overflow: 'hidden',
                                                        textOverflow: 'ellipsis',
                                                    }}>
                                                        HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho
                                                    </Typography>

                                                    <Box sx={{ display: "flex", gap: "15px" }}>
                                                        <Box sx={{
                                                            display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                            '&:hover': {
                                                                color: "blue"
                                                            }
                                                        }}>
                                                            <LocalPhoneOutlinedIcon />
                                                            <a href='tel:0325845628'><Typography sx={{
                                                                '&:hover': {
                                                                    color: "blue"
                                                                }
                                                            }}>0325845628</Typography></a>
                                                        </Box>
                                                        <Box sx={{
                                                            display: "flex", alignItems: "center", gap: "10px", mt: 2,
                                                            '&:hover': {
                                                                color: "blue"
                                                            }
                                                        }}>
                                                            <EmailOutlinedIcon />
                                                            <a href='mailto:xuanthulab.net@gmail.com'>
                                                                <Typography
                                                                    sx={{
                                                                        whiteSpace: 'nowrap',
                                                                        overflow: 'hidden',
                                                                        textOverflow: 'ellipsis',
                                                                        maxWidth: '180px',
                                                                        '&:hover': {
                                                                            color: "blue"
                                                                        }
                                                                    }}
                                                                >
                                                                    nguyenthuytrang@gmail.com
                                                                </Typography>
                                                            </a>
                                                        </Box>

                                                    </Box>
                                                    <Typography mt={4} variant='h5'>{formatter.format(100000)} / buổi</Typography>
                                                </CardContent>
                                                <CardActions>
                                                    <Button sx={{ fontSize: "20px" }}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                                </CardActions>
                                            </Box>
                                        </Card>
                                    </Grid>))}

                            </Grid>
                        </Grid>

                        <Grid item xs={2}></Grid>
                    </Grid>

                </Grid>
                <Grid item xs={2} />
                <Grid item xs={12} mt={5} sx={{ display: "flex", justifyContent: "center" }}>
                    {visibleCards < arrCenter.length && (
                        <Grid item xs={12} mt={5} sx={{ display: "flex", justifyContent: "center" }}>
                            <ButtonComponent action={handleShowMore} width={"200px"} height={"50px"} text={"Xem thêm"} borderRadius={"20px"} />
                        </Grid>
                    )}
                </Grid>
            </Grid>
        </Box>
    );
}

export default SearchTutor;
