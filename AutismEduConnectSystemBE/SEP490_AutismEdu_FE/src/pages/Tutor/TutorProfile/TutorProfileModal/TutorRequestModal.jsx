import ForwardToInboxIcon from '@mui/icons-material/ForwardToInbox';
import { Box, Button, Dialog, DialogContent, DialogTitle, Divider, FormControl, Grid, MenuItem, Select, TextField, Typography } from '@mui/material';
import { format } from 'date-fns';
import { Form, Formik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import * as Yup from 'yup';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { userInfor } from '~/redux/features/userSlice';
import ChildCreation from './ChildCreation';
import ParentSetupProfile from './ParentSetupProfile';

function TutorRequestModal({ rejectChildIds, tutorId, calculateAge }) {
    const [loading, setLoading] = useState(false);
    const [open, setOpen] = useState(false);
    const [childData, setChildData] = useState([]);
    const [selectedChild, setSelectedChild] = useState(null);
    const userInf = useSelector(userInfor);
    const [studyingList, setStudyingList] = useState([]);
    const [modalOpen, setModalOpen] = useState(0);
    const nav = useNavigate();

    const menuProps = {
        PaperProps: {
            style: {
                maxHeight: 200,
                overflowY: 'auto'
            }
        }
    };

    const handleOpen = async () => {
        if (!userInf) {
            nav('/autismedu/login', { state: { tutorId } });
        } else if (!userInf?.address || !userInf?.phoneNumber) {
            enqueueSnackbar('Bạn cần cập nhật thêm thông tin cá nhân!', { variant: 'warning' });
            setOpen(true)
            // nav(PAGES.ROOT + PAGES.PARENT_PROFILE);
            setModalOpen(1);
        } else {
            await handleGetChildInformation();
        }
    };
    const handleClose = () => setOpen(false);

    useEffect(() => {
        if (userInf) {
            handleGetStudyingList();
        }
    }, [userInf]);

    const handleGetStudyingList = async () => {
        try {
            await services.StudentProfileAPI.getMyTutor((res) => {
                if (res?.result) {
                    const newData = res?.result?.filter((r) => r.tutorId === tutorId);
                    setStudyingList(newData);
                }
            }, (error) => {
                console.log(error);
            }, {
                status: 'teaching'
            });
        } catch (error) {
            console.log(error);
        }
    };

    const handleGetChildInformation = async () => {
        try {
            await services.ChildrenManagementAPI.listChildren(userInf?.id, (res) => {
                if (res?.result) {

                    if (res.result?.length === 0) {
                        if (open === false) {
                            setOpen(true)
                        }
                        setModalOpen(2);
                    } else {
                        setModalOpen(0);
                        const x = res.result?.sort((a, b) => (b.id - a.id))?.find((r) => !(rejectChildIds?.includes(r?.id) || studyingList.some((s) => (s?.childId === r?.id))));

                        if (!x) {
                            enqueueSnackbar(
                                'Hiện tại không có trẻ nào của bạn phù hợp với gia sư này.'
                                ,
                                { variant: 'warning' }
                            );
                            setOpen(false);
                        } else {
                            setSelectedChild(x);
                            setChildData(res.result);
                            setOpen(true);
                        }
                    }

                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }
    };


    const handleSaveRequest = async (note) => {
        const requestData = {
            tutorId: tutorId,
            childId: selectedChild?.id,
            description: note
        };

        try {
            setLoading(true);
            await services.TutorRequestAPI.createTutorRequest(requestData, (res) => {
                setChildData([]);
                enqueueSnackbar("Gửi yêu cầu tới gia sư thành công!", { variant: "success" });
                handleClose();
            }, (error) => {
                // if (error?.code === 400) {
                    enqueueSnackbar(error.error[0], { variant: "error" });
                    handleClose();
                // }
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const validationSchema = Yup.object({
        note: Yup.string()
            .trim('Không được để trống hoặc chứa toàn khoảng trắng')
            .strict()
            .min(10, 'Phải nhập tối thiểu 10 ký tự')
            .max(1000, 'Không được nhập quá 1000 ký tự')
            .required('Vui lòng nhập ghi chú')
    });

    const formatDate = (dateString) => {
        return format(new Date(dateString), 'dd/MM/yyyy');
    };

    const isDisplayChild = (id) => {
        const rejectCase = rejectChildIds?.includes(id);
        const studyingCase = studyingList.some(s => s.childId === id);
        return rejectCase || studyingCase;
    };

    const checkChildValidate = (id) => {
        const rejectCase = rejectChildIds?.includes(id);
        const studyingCase = studyingList.some(s => s.childId === id);

        const resultStatus = rejectCase ? 'Từ chối' : studyingCase ? 'Đang học' : '';
        return resultStatus;
    };

    return (
        <>
            <Button onClick={handleOpen} startIcon={<ForwardToInboxIcon />} variant='contained' color='primary' size='large'>
                Gửi yêu cầu
            </Button>
            {
                modalOpen === 0 && (
                    <Dialog
                        open={open}
                        onClose={handleClose}
                        maxWidth="md"
                        fullWidth
                        aria-labelledby="dialog-title"
                        aria-describedby="dialog-description"
                    >
                        <DialogTitle id="dialog-title" variant='h5' textAlign={'center'}>Gửi yêu cầu cho gia sư</DialogTitle>
                        <Divider />
                        <DialogContent>
                            <Grid container spacing={2} mt={0}>
                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Chọn trẻ của bạn:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <FormControl fullWidth margin="dense">
                                            <Select
                                                MenuProps={menuProps}
                                                value={selectedChild?.id || ''}
                                                onChange={(e) => {
                                                    const selected = childData?.find(child => child?.id === e.target.value);
                                                    setSelectedChild(selected);
                                                }}
                                            >
                                                {childData?.map(child => (
                                                    <MenuItem
                                                        key={child?.id}
                                                        value={child?.id}
                                                        disabled={rejectChildIds?.includes(child?.id) || studyingList.some(s => s?.childId === child?.id)}
                                                    >
                                                        {child?.name} {isDisplayChild(child?.id) && <Typography ml={1} component={'span'} fontWeight={'bold'} color={checkChildValidate(child?.id) === 'Đang học' ? 'green' : 'red'}>({checkChildValidate(child?.id)})</Typography>}
                                                    </MenuItem>
                                                ))}
                                            </Select>
                                        </FormControl>
                                    </Grid>
                                </Grid>
                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Số điện thoại:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography variant='subtitle1'>{userInf?.phoneNumber || '0338581585'}</Typography>
                                    </Grid>
                                </Grid>

                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Tên trẻ:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography variant='subtitle1'>{selectedChild?.name || 'K'}</Typography>
                                    </Grid>
                                </Grid>

                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Giới tính:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography variant='subtitle1'>{selectedChild?.gender && (selectedChild?.gender === 'Male' ? 'Nam' : 'Nữ')}</Typography>
                                    </Grid>
                                </Grid>

                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Ngày sinh:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography variant='subtitle1'>{selectedChild?.birthDate && formatDate(selectedChild?.birthDate)}</Typography>
                                    </Grid>
                                </Grid>

                                <Grid item xs={12} container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Tuổi:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography variant='subtitle1'>{selectedChild?.birthDate && calculateAge(new Date(selectedChild?.birthDate))}</Typography>
                                    </Grid>
                                </Grid>

                                <Grid item xs={12} container spacing={2}>
                                    <Grid item xs={4}>
                                        <Typography variant='subtitle1'>Tình trạng của trẻ hiện tại:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Formik
                                            initialValues={{ note: '' }}
                                            validationSchema={validationSchema}
                                            onSubmit={(values) => {
                                                handleSaveRequest(values.note);
                                            }}
                                        >
                                            {({ values, errors, touched, handleChange, handleBlur }) => (
                                                <Form>
                                                    <TextField
                                                        name="note"
                                                        label="Tình trạng của trẻ hiện tại"
                                                        multiline
                                                        rows={6}
                                                        value={values.note}
                                                        onChange={handleChange}
                                                        onBlur={handleBlur}
                                                        error={touched.note && Boolean(errors.note)}
                                                        helperText={touched.note && errors.note}
                                                        fullWidth
                                                    />
                                                    <Box textAlign="right">
                                                        <Typography variant="caption" color={values.note.length > 1000 ? 'error' : 'textSecondary'}>
                                                            {`${values.note.length}/1000`}
                                                        </Typography>
                                                    </Box>
                                                    <Box mt={3} display="flex" justifyContent="right">
                                                        <Button variant="outlined" color="inherit" onClick={handleClose} sx={{ mr: 2 }}>
                                                            Hủy
                                                        </Button>
                                                        <Button variant="contained" color="primary" type="submit">
                                                            Lưu
                                                        </Button>
                                                    </Box>
                                                </Form>
                                            )}
                                        </Formik>
                                    </Grid>
                                </Grid>

                            </Grid>
                        </DialogContent>
                        <LoadingComponent open={loading} setOpen={setLoading} />
                    </Dialog>
                )
            }
            {
                modalOpen === 1 && (
                    <ParentSetupProfile handleClose={handleClose} loading={loading}
                        setLoading={setLoading}
                        setModalOpen={setModalOpen}
                        open={open}
                        handleGetChildInformation={handleGetChildInformation}
                    />
                )
            }
            {
                modalOpen === 2 && (
                    <ChildCreation handleClose={handleClose} loading={loading}
                        setLoading={setLoading}
                        setModalOpen={setModalOpen}
                        open={open}
                        setChild={setChildData}
                        handleGetChildInformation={handleGetChildInformation}
                    />
                )
            }
        </>
    );
}

export default TutorRequestModal;