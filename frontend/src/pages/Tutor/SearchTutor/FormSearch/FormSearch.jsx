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
function FormSearch({ selected, setSelected, showFilters, handleSearch, handleFilterClick, searchCriteria, setSearchCriteria }) {
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [selectedProvince, setSelectedProvince] = useState('');
    const [selectedDistrict, setSelectedDistrict] = useState('');
    const [selectedCommune, setSelectedCommune] = useState('');
    const [selectedRating, setSelectedRating] = useState('');
    const [loadingDistricts, setLoadingDistricts] = useState(false);
    const [loadingCommunes, setLoadingCommunes] = useState(false);
    useEffect(() => {
        getDataProvince();
    }, []);

    const getDataProvince = async () => {
        setProvinces(await CALL_API_ADDRESS('province'));
    };
    console.log(searchCriteria);

    const handleProvinceChange = (event) => {
        const provinceData = event.target.value;
        let arrProvince = provinceData.split("|");
        let [provinceId, provinceName] = arrProvince;

        setSelectedProvince(provinceId + "|" + provinceName);
        setSelectedDistrict('');
        setSelectedCommune('');  
        setDistricts([]);       
        setCommunes([]);         
        setLoadingDistricts(true); 

        axios.get(`https://vietnam-administrative-division-json-server-swart.vercel.app/district?idProvince=${provinceId}`)
            .then(response => {
                setDistricts(response.data);
                setLoadingDistricts(false);
            })
            .catch(error => {
                console.error('Error fetching districts:', error);
                setLoadingDistricts(false);
            });

        setSearchCriteria(prev => ({
            ...prev,
            address: `${provinceName}`
        }));
    };

    const handleDistrictChange = (event) => {
        const districtData = event.target.value;
        let arrDistrict = districtData.split("|");
        let [districtId, districtName] = arrDistrict;

        setSelectedDistrict(districtId + "|" + districtName);
        setSelectedCommune(''); 
        setCommunes([]);       
        setLoadingCommunes(true); 

        axios.get(`https://vietnam-administrative-division-json-server-swart.vercel.app/commune?idDistrict=${districtId}`)
            .then(response => {
                setCommunes(response.data);
                setLoadingCommunes(false);
            })
            .catch(error => {
                console.error('Error fetching communes:', error);
                setLoadingCommunes(false);
            });

        setSearchCriteria(prev => ({
            ...prev,
            address: `${districtName}|${selectedProvince.split("|")[1]}` 
        }));
    };

    const handleCommuneChange = (event) => {
        const communeData = event.target.value;
        let arrCommune = communeData.split("|");
        let [communeId, communeName] = arrCommune;

        setSelectedCommune(communeId + "|" + communeName);

        setSearchCriteria(prev => ({
            ...prev,
            address: `${communeName}|${selectedDistrict.split("|")[1]}|${selectedProvince.split("|")[1]}` // Thêm tỉnh, quận/huyện, và phường/xã
        }));
    };
    function handleClick(event) {
        event.preventDefault();
        console.info('You clicked a breadcrumb.');
    };
    const handleButtonClick = (button) => {
        setSelected(button);
    };
    const handleSearchChange = (e) => {
        setSearchCriteria(prev => ({
            ...prev,
            searchValue: e.target.value
        }));
    };
    const handleRatingChange = (e) => {
        setSearchCriteria(prev => ({
            ...prev,
            selectedRating: e.target.value
        }));
    };
    const breadcrumbs = [
        <Link underline="hover" key="1" color="inherit" href="/" onClick={handleClick}>
            Trang chủ
        </Link>, ,
        <Typography key="3" sx={{ color: 'rgb(107, 115, 133)' }}>
            Danh sách gia sư
        </Typography>,
    ];
    return (
        <Box
            sx={{
                background: `linear-gradient(to bottom, #f4f4f6, transparent),linear-gradient(to right, #4468f1, #c079ea)`,
                height: showFilters ? '630px' : '550px',
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
                            Tất cả các gia sư
                        </Typography>
                        <Typography sx={{ width: "55%" }} variant='subtitle1'>Gia sư đóng vai trò quan trọng trong việc hỗ trợ và giáo dục trẻ tự kỷ. Họ là những người trực tiếp dạy trẻ bên cạnh đó không chỉ giúp các em phát triển kỹ năng học tập mà còn hỗ trợ về mặt giao tiếp, tương tác xã hội, và các kỹ năng sống cần thiết.</Typography>
                    </Box>
                    <Box mt={4} sx={{ padding: '0px', color: "white" }}>
                        <Grid container spacing={0}>
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
                                            value={searchCriteria.searchValue}
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
                            <Grid item xs={12} my={3} sx={{ padding: 0 }}>
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
                                        <InputLabel>Tỉnh/Thành phố</InputLabel>
                                        <Select
                                            value={selectedProvince}
                                            onChange={handleProvinceChange}
                                            label="Tỉnh/Thành phố"
                                        >
                                            {provinces.map(province => (
                                                <MenuItem key={province.idProvince} value={province.idProvince + "|" + province.name}>
                                                    {province.name}
                                                </MenuItem>
                                            ))}
                                        </Select>
                                    </FormControl>

                                    {/* Select Quận/Huyện */}
                                    <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }} disabled={!selectedProvince}>
                                        <InputLabel>Quận/Huyện</InputLabel>
                                        <Select
                                            value={selectedDistrict}
                                            onChange={handleDistrictChange}
                                            label="Quận/Huyện"
                                            disabled={!selectedProvince} 
                                        >
                                            {loadingDistricts ? (
                                                <MenuItem disabled>
                                                    <CircularProgress size={24} />
                                                </MenuItem>
                                            ) : (
                                                districts.map(district => (
                                                    <MenuItem key={district.idDistrict} value={district.idDistrict + "|" + district.name}>
                                                        {district.name}
                                                    </MenuItem>
                                                ))
                                            )}
                                        </Select>
                                    </FormControl>

                                    {/* Select Phường/Xã */}
                                    <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }} disabled={!selectedDistrict}>
                                        <InputLabel>Phường/Xã</InputLabel>
                                        <Select
                                            value={selectedCommune}
                                            onChange={handleCommuneChange}
                                            label="Phường/Xã"
                                            disabled={!selectedDistrict} 
                                        >
                                            {loadingCommunes ? (
                                                <MenuItem disabled>
                                                    <CircularProgress size={24} />
                                                </MenuItem>
                                            ) : (
                                                communes.map(commune => (
                                                    <MenuItem key={commune.idCommune} value={commune.idCommune + "|" + commune.name}>
                                                        {commune.name}
                                                    </MenuItem>
                                                ))
                                            )}
                                        </Select>
                                    </FormControl>

                                    <FormControl fullWidth variant="outlined" sx={{ backgroundColor: "white", borderRadius: "5px" }}>
                                        <InputLabel>Đánh giá</InputLabel>
                                        <Select
                                            value={searchCriteria?.selectedRating}
                                            onChange={handleRatingChange}
                                            label="Đánh giá"
                                        >
                                            <MenuItem value={1} sx={{ display: 'flex', alignItems: 'center' }}>
                                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 1 }} /> <Typography variant='span'>1</Typography></Stack>
                                            </MenuItem>
                                            <MenuItem value={2} sx={{ display: 'flex', alignItems: 'center' }}>
                                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 1 }} /> <Typography variant='span'>2</Typography></Stack>
                                            </MenuItem>
                                            <MenuItem value={3} sx={{ display: 'flex', alignItems: 'center' }}>
                                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 1 }} /> <Typography variant='span'>3</Typography></Stack>
                                            </MenuItem>
                                            <MenuItem value={4} sx={{ display: 'flex', alignItems: 'center' }}>
                                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 1 }} /> <Typography variant='span'>4</Typography></Stack>
                                            </MenuItem>
                                            <MenuItem value={5} sx={{ display: 'flex', alignItems: 'center' }}>
                                                <Stack direction={"row"} alignItems={"center"}><StarIcon sx={{ color: 'gold', mr: 1 }} /> <Typography variant='span'>5</Typography></Stack>
                                            </MenuItem>
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
    )
}

export default FormSearch