import { Grid, Stack, Typography, TextField, Button, Box, MenuItem, Select, CircularProgress, FormControl, InputLabel, FormHelperText } from '@mui/material';
import React, { useEffect, useState } from 'react';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import ModalConfirm from './ModalConfirm';
import { useSelector } from 'react-redux';
import { tutorInfor } from '~/redux/features/tutorSlice';
import services from '~/plugins/services';
import axios from 'axios';
import PropTypes from 'prop-types';
import { NumericFormat } from 'react-number-format';
import CALL_API_ADDRESS from '~/utils/call_api_address';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';
import '../../../../assets/css/ql-editor.css';

const NumericFormatCustom = (props) => {
    const { inputRef, onChange, ...other } = props;
    return (
        <NumericFormat
            {...other}
            getInputRef={inputRef}
            thousandSeparator="."
            decimalSeparator=","
            allowNegative={false}
            onValueChange={(values) => {
                onChange({
                    target: {
                        name: props.name,
                        value: values.value,
                    },
                });
            }}
        />
    );
};


function EditProfile() {
    const [openConfirm, setOpenConfirm] = useState(false);
    const tutorInfo = useSelector(tutorInfor);
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [selectedProvince, setSelectedProvince] = useState('');
    const [selectedDistrict, setSelectedDistrict] = useState('');
    const [selectedCommune, setSelectedCommune] = useState('');
    const [specificAddress, setSpecificAddress] = useState('');
    const [loadingDistricts, setLoadingDistricts] = useState(false);
    const [loadingCommunes, setLoadingCommunes] = useState(false);
    const [tutor, setTutor] = useState(null);

    const [defaultTutor, setDefaultTutor] = useState(null);
    const [loading, setLoading] = useState(false);
    const menuProps = {
        PaperProps: {
            style: {
                maxHeight: 200,
                overflowY: 'auto',
            },
        },
    };

    const [isSaveDisabled, setIsSaveDisabled] = useState(true);
    const [errors, setErrors] = useState({});

    const validateForm = () => {

        const {
            priceFrom,
            priceEnd,
            sessionHours,
            startAge,
            endAge,
            phoneNumber,
            aboutMe
        } = tutor;

        const newErrors = {};

        if (priceFrom < 10000) {
            newErrors.priceFrom = 'Học phí từ phải lớn hơn bằng 10,000';
        }
        if (priceFrom > 10000000) {
            newErrors.priceFrom = 'Học phí phải nhỏ hơn 10.000.000';
        }
        if (priceEnd > 10000000) {
            newErrors.priceEnd = 'Học phí nhỏ hơn 10.000.000';
        }
        if (priceEnd <= priceFrom) {
            newErrors.priceEnd = 'Học phí đến phải lớn hơn học phí từ';
        }
        if (sessionHours <= 0 || sessionHours > 10) {
            newErrors.sessionHours = 'Số giờ dạy phải lớn hơn 0 và nhỏ hơn 10';
        }
        if (sessionHours > 10) {
            newErrors.sessionHours = 'Số giờ dạy phải nhỏ hơn 10';
        }
        if (startAge < 0) {
            newErrors.startAge = 'Tuổi từ phải lớn hơn hoặc bằng 0';
        }
        if (endAge < startAge) {
            newErrors.endAge = 'Tuổi đến phải lớn hơn tuổi từ';
        }
        if (startAge > 15) {
            newErrors.startAge = 'Tuổi bắt đầu phải nhỏ hơn 15 tuổi';
        }
        if (endAge > 15) {
            newErrors.endAge = 'Tuổi đến phải nhỏ hơn 15 tuổi';
        }
        const phoneRegex = /^[0-9]{10,11}$/;
        if (phoneNumber && !phoneRegex.test(phoneNumber)) {
            newErrors.phoneNumber = 'Số điện thoại không hợp lệ. Phải là 10 hoặc 11 chữ số.';
        }
        if (aboutMe.replace(/<(.|\n)*?>/g, '').trim().length > 5000) {
            newErrors.aboutMe = 'Không được vượt quá 5000 ký tự';
        }
        if (specificAddress.length > 100) {
            newErrors.specificAddress = 'Không được vượt quá 100 ký tự';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    useEffect(() => {
        if (tutor) {
            setIsSaveDisabled(!validateForm());
        }
    }, [tutor]);


    const handleInputChange = (e) => {
        const { name, value } = e.target;
        const nameList = ["priceFrom", "priceEnd", "sessionHours", "startAge", "endAge"];

        setTutor({
            ...tutor,
            [name]: nameList.includes(name) ? parseFloat(value) : value
        });
    };

    const handleQuillChange = (value) => {
        setTutor({
            ...tutor,
            aboutMe: value
        });
    };

    const getTutorInformation = async () => {
        try {
            await services.TutorManagementAPI.handleGetTutorProfile((res) => {
                if (res?.result) {
                    setDefaultTutor(res.result);
                    setTutor(res.result);
                    if (res?.result?.address) {
                        const [provinceName, districtName, communeName, address] = res.result.address.split('|');
                        setSpecificAddress(address);
                        setSelectedProvince(provinceName);
                        fetchCommunes(districtName, communeName);
                    }
                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }
    };

    const getDataProvince = async () => {
        setProvinces(await CALL_API_ADDRESS('province'));
    };

    useEffect(() => {
        getDataProvince();
        getTutorInformation();
    }, []);

    useEffect(() => {
        if (provinces.length > 0 && selectedProvince) {
            fetchDistricts(selectedProvince, tutor?.address?.split('|')[1]);
        }
    }, [provinces, selectedProvince]);
    useEffect(() => {
        if (districts.length > 0 && selectedDistrict) {
            fetchCommunes(selectedDistrict, tutor?.address?.split('|')[2]);
        }
    }, [districts, selectedDistrict]);

    const fetchDistricts = (provinceName, districtName = '') => {
        if (!provinces || provinces.length === 0) return;

        const selectedProvinceData = provinces.find(prov => prov.name === provinceName);
        if (selectedProvinceData) {
            setLoadingDistricts(true);
            axios.get(`https://vietnam-administrative-division-json-server-swart.vercel.app/district?idProvince=${selectedProvinceData.idProvince}`)
                .then(response => {
                    setDistricts(response.data);
                    setLoadingDistricts(false);
                    if (districtName) {
                        const selectedDistrictData = response.data.find(d => d.name === districtName);
                        if (selectedDistrictData) {
                            setSelectedDistrict(selectedDistrictData.name);
                        }
                    }
                })
                .catch(error => {
                    setLoadingDistricts(false);
                    console.error('Error fetching districts:', error);
                });
        }
    };

    const fetchCommunes = (districtName, communeName = '') => {
        if (!districts || districts.length === 0) return;
        const selectedDistrictData = districts.find(district => district.name === districtName);
        console.log(selectedDistrictData);

        if (selectedDistrictData) {
            setLoadingCommunes(true);
            axios.get(`https://vietnam-administrative-division-json-server-swart.vercel.app/commune?idDistrict=${selectedDistrictData?.idDistrict}`)
                .then(response => {
                    setCommunes(response.data);
                    setLoadingCommunes(false);
                    if (communeName) {
                        const selectedCommuneData = response?.data?.find(c => c.name === communeName);

                        if (selectedCommuneData) {

                            setSelectedCommune(selectedCommuneData.name);
                        }
                    }
                })
                .catch(error => {
                    setLoadingCommunes(false);
                    console.error('Error fetching communes:', error);
                });
        }
    };

    const handleProvinceChange = (event) => {
        const provinceName = event.target.value;
        setSelectedProvince(provinceName);
        setSelectedDistrict('');
        setSelectedCommune('');
        fetchDistricts(provinceName);
    };

    const handleDistrictChange = (event) => {
        const districtName = event.target.value;
        setSelectedDistrict(districtName);
        setSelectedCommune('');
        fetchCommunes(districtName);
    };

    const handleCommuneChange = (event) => {
        setSelectedCommune(event.target.value);
    };


    const handleSaveClick = async (e) => {
        e.preventDefault()
        if (validateForm()) {
            const updatedAddress = `${selectedProvince}|${selectedDistrict}|${selectedCommune}|${specificAddress}`;
            const updateTutor = {
                priceFrom: parseFloat(tutor.priceFrom),
                priceEnd: parseFloat(tutor.priceEnd),
                sessionHours: tutor.sessionHours,
                address: updatedAddress,
                aboutMe: tutor.aboutMe,
                phoneNumber: tutor.phoneNumber,
                startAge: tutor.startAge,
                endAge: tutor.endAge
            };
            try {
                setLoading(true);
                await services.TutorManagementAPI.handleUpdateTutorProfile(tutorInfo?.id, updateTutor, (res) => {
                    console.log(res);
                    if (res?.result) {
                        setTutor(res.result);
                    }
                    enqueueSnackbar('Cập nhật đã được gửi thành công đến hệ thống!\n', { variant: 'success' });
                }, (error) => {
                    console.log(error);
                });
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false);
            }
        }
    };
    const handleChangeSpecificAddress = (e) => {
        const updatedAddress = `${selectedProvince}|${selectedDistrict}|${selectedCommune}|${e.target.value}`;
        setSpecificAddress(e.target.value);
        setTutor((prev) => ({ ...prev, address: updatedAddress }));
    };

    const checkChangeValue = () => {
        let isNotChange = true;
        if (tutor && defaultTutor) {
            isNotChange = !(tutor?.priceEnd !== defaultTutor?.priceEnd || tutor?.priceFrom !== defaultTutor?.priceFrom || tutor?.sessionHours !== defaultTutor?.sessionHours ||
                tutor?.startAge !== defaultTutor?.startAge || tutor?.endAge !== defaultTutor?.endAge || tutor?.phoneNumber !== defaultTutor?.phoneNumber || tutor?.address !== defaultTutor?.address
                || tutor?.aboutMe !== defaultTutor?.aboutMe || selectedCommune !== defaultTutor.address.split('|')[2]);
        }
        return isNotChange;
    };

    return (
        <Stack direction='column' sx={{ width: "90%", margin: "auto", mt: "20px", gap: 2 }}>
            <Stack direction={'row'} spacing={2} mb={5}>
                <Typography variant='h4' my={2}>Chỉnh sửa hồ sơ </Typography> {(tutor?.requestStatus === 2) ? <Button size='small' variant='outlined' color='warning'>Đang chờ duyệt</Button> :
                    <Button size='small' variant='outlined' color='success'>Đã chấp nhận</Button>}
            </Stack>
            <Grid container spacing={3} component="form" onSubmit={handleSaveClick}>
                <Grid item xs={6} md={3}>
                    <TextField
                        fullWidth
                        required
                        label="Học phí từ (VNĐ)"
                        variant="outlined"
                        name="priceFrom"
                        value={tutor?.priceFrom || ''}
                        onChange={handleInputChange}
                        type="text"
                        error={!!errors.priceFrom}
                        InputProps={{
                            inputComponent: NumericFormatCustom,
                        }}
                    />
                    {errors.priceFrom && (
                        <FormHelperText error>{errors.priceFrom}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={6} md={3}>
                    <TextField
                        fullWidth
                        required
                        label="Đến (VNĐ)"
                        variant="outlined"
                        name="priceEnd"
                        value={tutor?.priceEnd || ''}
                        onChange={handleInputChange}
                        type="text"
                        error={!!errors.priceEnd}
                        InputProps={{
                            inputComponent: NumericFormatCustom,
                        }}
                    />
                    {errors.priceEnd && (
                        <FormHelperText error>{errors.priceEnd}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={12} md={6}>
                    <TextField
                        fullWidth
                        label="Email"
                        variant="outlined"
                        name="email"
                        value={tutorInfo?.email || ''}
                        onChange={handleInputChange}
                    />
                </Grid>

                <Grid item xs={4} md={2}>
                    <TextField
                        fullWidth
                        required
                        label="Số giờ dạy trên buổi"
                        variant="outlined"
                        name="sessionHours"
                        value={tutor?.sessionHours || ''}
                        onChange={handleInputChange}
                        type="number"
                        error={!!errors.sessionHours}
                    />
                    {errors.sessionHours && (
                        <FormHelperText error>{errors.sessionHours}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={4} md={2}>
                    <TextField
                        fullWidth
                        required
                        label="Tuổi từ"
                        variant="outlined"
                        name="startAge"
                        value={tutor?.startAge || ''}
                        onChange={handleInputChange}
                        type="number"
                        error={!!errors.startAge}
                    />
                    {errors.startAge && (
                        <FormHelperText error>{errors.startAge}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={4} md={2}>
                    <TextField
                        fullWidth
                        required
                        label="Đến"
                        variant="outlined"
                        name="endAge"
                        value={tutor?.endAge || ''}
                        onChange={handleInputChange}
                        type="number"
                        error={!!errors.endAge}
                    />
                    {errors.endAge && (
                        <FormHelperText error>{errors.endAge}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={12} md={6}>
                    <TextField
                        fullWidth
                        required
                        label="Số điện thoại"
                        variant="outlined"
                        name="phoneNumber"
                        value={tutor?.phoneNumber || ''}
                        onChange={handleInputChange}
                        error={!!errors.phoneNumber}
                    />
                    {errors.phoneNumber && (
                        <FormHelperText error>{errors.phoneNumber}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={12} md={6}>
                    <FormControl fullWidth variant="outlined">
                        <InputLabel>Tỉnh/Thành phố</InputLabel>
                        <Select
                            required
                            value={selectedProvince || ''}
                            onChange={handleProvinceChange}
                            label="Tỉnh/Thành phố"
                            MenuProps={menuProps}
                        >
                            {provinces.map(province => (
                                <MenuItem key={province.idProvince} value={province.name}>
                                    {province.name}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                </Grid>

                <Grid item xs={12} md={6}>
                    <FormControl fullWidth variant="outlined" disabled={!selectedProvince}>
                        <InputLabel>Quận/Huyện</InputLabel>
                        <Select
                            required
                            value={selectedDistrict || ''}
                            onChange={handleDistrictChange}
                            label="Quận/Huyện"
                            MenuProps={menuProps}
                        >
                            {loadingDistricts ? (
                                <MenuItem disabled>
                                    <CircularProgress size={24} />
                                </MenuItem>
                            ) : (
                                districts.map(district => (
                                    <MenuItem key={district.idDistrict} value={district.name}>
                                        {district.name}
                                    </MenuItem>
                                ))
                            )}
                        </Select>
                    </FormControl>
                </Grid>

                <Grid item xs={12} md={6}>
                    <FormControl fullWidth variant="outlined" disabled={!selectedDistrict}>
                        <InputLabel>Xã/Phường</InputLabel>
                        <Select
                            required
                            value={selectedCommune || ''}
                            onChange={handleCommuneChange}
                            label="Xã/Phường"
                            MenuProps={menuProps}
                        >
                            {loadingCommunes ? (
                                <MenuItem disabled>
                                    <CircularProgress size={24} />
                                </MenuItem>
                            ) : (
                                communes.map(commune => (
                                    <MenuItem key={commune.idCommune} value={commune.name}>
                                        {commune.name}
                                    </MenuItem>
                                ))
                            )}
                        </Select>
                    </FormControl>
                </Grid>

                <Grid item xs={12} md={6}>
                    <TextField
                        required
                        fullWidth
                        label="Địa chỉ cụ thể"
                        variant="outlined"
                        name="specificAddress"
                        value={specificAddress}
                        onChange={(e) => handleChangeSpecificAddress(e)}
                    />
                    {errors.specificAddress && (
                        <FormHelperText error>{errors.specificAddress}</FormHelperText>
                    )}
                </Grid>

                <Grid item xs={12} mt={0} sx={{ height: '350px' }}>
                    <Typography variant='h6' mb={2}>Giới thiệu về tôi</Typography>
                    <ReactQuill
                        value={tutor?.aboutMe || ''}
                        onChange={handleQuillChange}
                    />
                    {errors.aboutMe ? (
                        <FormHelperText error >{errors.aboutMe}</FormHelperText>
                    ) : <Typography variant="body2" sx={{ mt: 1 }}>
                        {tutor?.aboutMe.replace(/<(.|\n)*?>/g, '').trim().length} / 5000
                    </Typography>}
                </Grid>

                <Grid item xs={12} mt={2}>
                    <Box textAlign='right'>
                        <Button disabled={tutor?.requestStatus === 2 || checkChangeValue() || isSaveDisabled} variant="contained" color="primary" type='submit'>Lưu</Button>
                    </Box>
                </Grid>
            </Grid>

            <LoadingComponent open={loading} setOpen={setLoading} />

            {openConfirm && <ModalConfirm open={openConfirm} onClose={() => setOpenConfirm(false)} handleSubmit={handleSaveClick} />}
        </Stack>
    );
}

export default EditProfile;
