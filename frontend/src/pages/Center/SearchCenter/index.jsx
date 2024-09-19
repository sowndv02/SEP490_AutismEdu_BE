import React, { useState } from 'react';
import { Grid, Box, TextField, InputAdornment, Card, CardContent, CardMedia, Typography, Rating, Button, Link, IconButton, CardActions, Container, Select, MenuItem, InputLabel, FormControl } from '@mui/material';
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
<<<<<<< HEAD
import ButtonComponent from '~/Components/ButtonComponent';
import Collapse from '@mui/material/Collapse';

function Index() {
=======
import ButtonComponent from '~/components/ButtonComponent';
import Collapse from '@mui/material/Collapse';
import FormSearch from './FormSearch/FormSearch';

function Index() {
    const [searchCriteria, setSearchCriteria] = useState({
        searchValue: "",
        address: "",
        selectedRating: ""
    });
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const [selected, setSelected] = useState('grid');
    const [searchValue, setSearchValue] = useState('');
    const arrCenter = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    const [visibleCards, setVisibleCards] = useState(6);
    const [showFilters, setShowFilters] = useState(false);

    const handleFilterClick = () => {
        setShowFilters(!showFilters);
    };
<<<<<<< HEAD
    const handleSearchChange = (event) => {
        setSearchValue(event.target.value);
    };
=======

>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
=======

    // Khi người dùng chọn đánh giá
    const handleRatingChange = (event) => {
        setSelectedRating(event.target.value);
    };
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const breadcrumbs = [
        <Link underline="hover" key="1" color="inherit" href="/" onClick={handleClick}>
            Trang chủ
        </Link>, ,
        <Typography key="3" sx={{ color: 'rgb(107, 115, 133)' }}>
            Danh sách trung tâm
        </Typography>,
    ];
<<<<<<< HEAD

    const [region, setRegion] = useState('');
    const [ward, setWard] = useState('');
    const [rating, setRating] = useState('');

    const handleRegionChange = (event) => {
        setRegion(event.target.value);
    };

    const handleWardChange = (event) => {
        setWard(event.target.value);
    };

    const handleRatingChange = (event) => {
        setRating(event.target.value);
    };
    return (
        <Box sx={{ height: 'auto' }}>
            <Box
                sx={{
                    background: `linear-gradient(to bottom, #f4f4f6, transparent),linear-gradient(to right, #4468f1, #c079ea)`,
                    height: showFilters?'600px':'550px',
                    transition: 'height 0.5s ease',
                }}
            >
                <Grid container sx={{ height: '100%' }}>
                    <Grid item xs={2} />

                    <Grid item xs={8}>
                        <Box mt={5} sx={{ padding: '10px' }}>
                            <Stack spacing={2}>
                                <Breadcrumbs separator="›" aria-label="breadcrumb">
                                    {breadcrumbs}
                                </Breadcrumbs>
                            </Stack>
                            <Typography my={3} variant='h3' fontWeight={'bold'} color={"#192335"}>
                                Tất cả các trung tâm
                            </Typography>
                            <Typography sx={{ width: "55%" }} variant='subtitle1'>Các trung tâm này bao gồm các tổ chức chuyên về giáo dục đặc biệt, liệu pháp ngôn ngữ, trị liệu hành vi và các chương trình đào tạo kỹ năng sống.</Typography>
                        </Box>
                        <Box mt={0} sx={{ padding: '20px', color: "white" }}>
                            <Grid container spacing={2}>
                                {/* Cột 4 */}
                                <Grid item xs={6}>
                                    <Box>
                                        {/* Nút "Lưới" */}
                                        <Button
                                            variant="contained"
                                            onClick={() => handleButtonClick('grid')}
                                            sx={{
                                                backgroundColor: selected === 'grid' ? '#fff' : 'transparent',
                                                color: selected === 'grid' ? '#2f57ef' : '#fff',
                                                borderRadius: '999px',
                                                boxShadow: selected === 'list' ? '0px 4px 10px rgba(0, 0, 0, 0.2)' : 'none',
                                                padding: '10px 20px',
                                                marginRight: '10px',
                                                '&:hover': {
                                                    backgroundColor: '#fff',
                                                    color: '#2f57ef',
                                                }
                                            }}
                                            startIcon={<GridViewIcon />}
                                        >
                                            Lưới
                                        </Button>

                                        {/* Nút "Danh sách" */}
                                        <Button
                                            variant="contained"
                                            onClick={() => handleButtonClick('list')}
                                            sx={{
                                                backgroundColor: selected === 'list' ? '#fff' : 'transparent',
                                                color: selected === 'list' ? '#2f57ef' : '#fff',
                                                borderRadius: '999px',
                                                boxShadow: selected === 'grid' ? '0px 4px 10px rgba(0, 0, 0, 0.2)' : 'none',
                                                padding: '10px 20px',
                                                '&:hover': {
                                                    backgroundColor: '#fff',
                                                    color: '#2f57ef',
                                                }
                                            }}
                                            startIcon={<FormatListBulletedIcon />}
                                        >
                                            Danh sách
                                        </Button>
                                    </Box>
                                </Grid>


                                {/* Cột 6 */}
                                <Grid item xs={6}>
                                    <Grid container spacing={2} alignItems="center">
                                        {/* Thanh tìm kiếm */}
                                        <Grid item xs={9}>
                                            <TextField
                                                variant="outlined"
                                                placeholder="Hãy nhập trung tâm mà bạn muốn tìm..."
                                                value={searchValue}
                                                onChange={handleSearchChange}
                                                fullWidth
                                                InputProps={{
                                                    endAdornment: (
                                                        <InputAdornment position="end">
                                                            <IconButton onClick={handleSearch}>
                                                                <SearchIcon />
                                                            </IconButton>
                                                        </InputAdornment>
                                                    ),
                                                    sx: {
                                                        height: '45px',
                                                        borderRadius: '999px',
                                                        backgroundColor: '#fff',
                                                    },
                                                }}
                                            />
                                        </Grid>

                                        {/* Nút bộ lọc */}
                                        <Grid item xs={3}>
                                            <Button
                                                variant="contained"
                                                startIcon={<FilterListIcon />}
                                                sx={{
                                                    borderRadius: '999px',
                                                    padding: '10px 20px',
                                                    backgroundColor: '#fff',
                                                    color: 'black',
                                                    width: '100%',
                                                    fontWeight: "600",
                                                    '&:hover': {
                                                        backgroundColor: 'transparent',
                                                        color: "white"
                                                    },
                                                }}
                                                onClick={handleFilterClick}
                                            >
                                                Bộ lọc
                                            </Button>
                                        </Grid>
                                    </Grid>
                                </Grid>
                                <Grid item xs={12} my={2}>
                                    <Box sx={{
                                        display: 'flex',
                                        justifyContent: 'space-between',
                                        alignItems: 'center',
                                        gap: 2,
                                        backgroundColor: '#fff',
                                        padding: '20px',
                                        borderRadius: '8px',
                                        boxShadow: '0px 4px 10px rgba(0, 0, 0, 0.1)', 
                                        maxHeight: showFilters ? '500px' : '0',
                                        opacity: showFilters ? 1 : 0, 
                                        overflow: 'hidden',
                                        transition: 'max-height 0.5s ease, opacity 0.5s ease',
                                    }}>
                                        <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }}>
                                            <InputLabel>Khu vực</InputLabel>
                                            <Select
                                                value={region}
                                                onChange={handleRegionChange}
                                                label="Khu vực"
                                            >
                                                <MenuItem value={1}>Khu vực 1</MenuItem>
                                                <MenuItem value={2}>Khu vực 2</MenuItem>
                                                <MenuItem value={3}>Khu vực 3</MenuItem>
                                            </Select>
                                        </FormControl>

                                        <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }}>
                                            <InputLabel>Phường xã</InputLabel>
                                            <Select
                                                value={ward}
                                                onChange={handleWardChange}
                                                label="Phường xã"
                                            >
                                                <MenuItem value={1}>Phường xã 1</MenuItem>
                                                <MenuItem value={2}>Phường xã 2</MenuItem>
                                                <MenuItem value={3}>Phường xã 3</MenuItem>
                                            </Select>
                                        </FormControl>

                                        <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }}>
                                            <InputLabel>Đánh giá</InputLabel>
                                            <Select
                                                value={rating}
                                                onChange={handleRatingChange}
                                                label="Đánh giá"
                                            >
                                                <MenuItem value={1}>1 sao</MenuItem>
                                                <MenuItem value={2}>2 sao</MenuItem>
                                                <MenuItem value={3}>3 sao</MenuItem>
                                                <MenuItem value={4}>4 sao</MenuItem>
                                                <MenuItem value={5}>5 sao</MenuItem>
                                            </Select>
                                        </FormControl>
                                    </Box>

                                </Grid>
                            </Grid>
                        </Box>
                    </Grid>

                    <Grid item xs={2} />
                </Grid>
            </Box>
            <Grid container sx={{ height: 'auto', position: "relative", top: '-180px' }}>

                <Grid item xs={2} />

=======
    return (
        <Box sx={{ height: 'auto' }}>
            <FormSearch selected={selected} setSelected={setSelected}
                showFilters={showFilters} handleSearch={handleSearch} handleFilterClick={handleFilterClick} searchCriteria={searchCriteria} setSearchCriteria={setSearchCriteria} />
            <Grid container sx={{ height: 'auto', position: "relative", top: '-180px' }}>
                <Grid item xs={2} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                <Grid item xs={8} sx={{ height: 'auto' }} mt={5}>
                    <Grid container sx={{ height: 'auto' }}>
                        <Grid item>
                            <Grid container spacing={5}>
                                {selected === "list" ?
                                    arrCenter.slice(0, visibleCards).map((c, index) =>
                                    (<Grid item xs={4}>
                                        <Card sx={{
                                            padding: "20px", minHeight: "670px",
                                            transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                            '&:hover': {
                                                transform: "scale(1.05) translateY(-10px)",
                                                boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                            },
                                            borderRadius: "5px"
                                        }}>
                                            <CardMedia
                                                sx={{ height: 240 }}
                                                image="https://touristjourney.com/wp-content/uploads/2020/10/Discover-the-One-Pillar-Pagoda-during-the-Insider-Hanoi-City-Tour-scaled-e1601972144150-1024x569.jpg"
                                                title="Hanoi"
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
                                                    Trung tâm trẻ tự kỷ Inova
                                                </Typography>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "20px" }}>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                                        <ClassOutlinedIcon />
                                                        <Typography><span>20 lớp</span></Typography>
                                                    </Box>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                                        <Person3OutlinedIcon />
                                                        <Typography><span>20 giáo viên</span></Typography>
                                                    </Box>
                                                </Box>
                                                <Typography mt={2} sx={{
                                                    display: '-webkit-box',
                                                    WebkitLineClamp: 2,
                                                    WebkitBoxOrient: 'vertical',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                }}>
                                                    HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện  được thiết kế cho
                                                </Typography>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                    <LocationOnIcon />
                                                    <Typography>Tỉnh/Thành phố: Hồ Chí Minh</Typography>
                                                </Box>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                    <LocalPhoneIcon />
                                                    <Typography>SĐT: 40404040404</Typography>
                                                </Box>
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
                                            minHeight: "240px"
                                        }}>

                                            <CardMedia
                                                sx={{ width: '40%', height: 'auto', borderRadius: '8px' }} // 2/5 của bố cục
                                                image="https://nhuminhplazahotel.com/wp-content/uploads/2023/03/choi-gi-o-da-nang-2-min-1048x565.jpg"
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
                                                        Trung tâm trẻ tự kỷ Inova
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "20px" }}>
                                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                                            <ClassOutlinedIcon />
                                                            <Typography><span>20 lớp</span></Typography>
                                                        </Box>
                                                        <Box sx={{ display: "flex", alignItems: "center", gap: "5px" }}>
                                                            <Person3OutlinedIcon />
                                                            <Typography><span>20 giáo viên</span></Typography>
                                                        </Box>
                                                    </Box>
                                                    <Typography mt={2} sx={{
                                                        display: '-webkit-box',
                                                        WebkitLineClamp: 2,
                                                        WebkitBoxOrient: 'vertical',
                                                        overflow: 'hidden',
                                                        textOverflow: 'ellipsis',
                                                    }}>
                                                        HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện được thiết kế cho HiStudy Education Theme của Rainbow là một công cụ WordPress thân thiện được thiết kế cho
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                        <LocationOnIcon />
                                                        <Typography>Tỉnh/Thành phố: Hồ Chí Minh</Typography>
                                                    </Box>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                        <LocalPhoneIcon />
                                                        <Typography>SĐT: 40404040404</Typography>
                                                    </Box>
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

export default Index;
