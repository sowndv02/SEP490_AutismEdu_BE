import React, { useEffect, useState } from 'react';
import { Box, Button, Grid, Stack, Typography, TextField, IconButton, Divider } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import ExerciseAdd from '../SyllabusModal/ExerciseAdd';
import services from '~/plugins/services';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import DeleteIcon from '@mui/icons-material/Delete';

export default function SyllabusAssign({ handleBack, selectedAssign, setListSyllabus, tutorProfile }) {
    const [loading, setLoading] = useState(false);
    const [exerciseTypes, setExerciseTypes] = useState([]);
    const [selectedList, setSelectedList] = useState([]);
    const [selectedClone, setSelectedClone] = useState([]);
    const [openModal, setOpenModal] = useState(false);

    const [ageFrom, setAgeFrom] = useState(selectedAssign?.ageFrom ?? null);
    const [ageEnd, setAgeEnd] = useState(selectedAssign?.ageEnd ?? null);
    console.log(selectedList);
    console.log(selectedClone);
    console.log(selectedAssign);

    useEffect(() => {
        handleGetAllExerciseType();
    }, []);

    const handleGetAllExerciseType = async () => {
        try {
            await services.ExerciseManagementAPI.getAllExerciseType(
                (res) => {
                    if (res?.result) {
                        setExerciseTypes(res.result);
                    }
                },
                (error) => {
                    console.log(error);
                },
                { search: '', orderBy: 'createdDate', sort: 'desc', pageSize: 0, pageNumber: 1 }
            );
        } catch (error) {
            console.log(error);
        }
    };

    const handleOpenModal = () => setOpenModal(true);
    const handleCloseModal = () => setOpenModal(false);


    useEffect(() => {
        formik.setFieldValue("syllabusExercises", selectedList);
    }, [selectedList]);

    const formik = useFormik({
        initialValues: {
            ageFrom: ageFrom,
            ageEnd: ageEnd,
            syllabusExercises: selectedList,
        },
        validationSchema: Yup.object({
            ageFrom: Yup.number()
                .required('Bắt buộc phải nhập')
                .min(tutorProfile?.startAge ?? 0, `Tuổi bắt đầu phải lớn hơn bằng ${tutorProfile?.startAge || 0}`)
                .max(((tutorProfile?.endAge ?? 1) - 1), `Tuổi bắt đầu phải nhỏ hơn bằng ${((tutorProfile?.endAge ?? 1) - 1)}`)
            ,
            ageEnd: Yup.number()
                .required('Bắt buộc phải nhập')
                .positive('Độ tuổi phải là số dương')
                .moreThan(Yup.ref('ageFrom'), 'Độ tuổi kết thúc phải lớn hơn độ tuổi bắt đầu')
                .max(tutorProfile?.endAge, `Tuổi kết thúc phải nhỏ hơn bằng ${tutorProfile?.endAge ?? 1}`),
            syllabusExercises: Yup.array()
                .min(1, 'Phải có ít nhất 1 loại bài tập và bài tập'),
        }),
        onSubmit: async (values) => {
            console.log("Form submitted with values:", values);
            try {
                setLoading(true);
                const data = {
                    id: selectedAssign?.id,
                    ageFrom: values.ageFrom,
                    ageEnd: values.ageEnd,
                    syllabusExercises: [...selectedList]
                };

                await services.SyllabusManagementAPI.assignExerciseSyllabus(selectedAssign?.id, data, (res) => {
                    if (res?.result) {
                        setListSyllabus((prev) => {
                            const updateData = prev.map((p) => {
                                if (p.id === res.result?.id) {
                                    p = res.result;
                                    return p;
                                } else {
                                    return p;
                                }
                            });
                            return updateData;
                        });
                        enqueueSnackbar("Gán bài tập thành công!", { variant: 'success' });
                        setSelectedClone([]);
                        setSelectedList([]);
                        formik.resetForm();
                        handleBack();
                    }
                }, (error) => console.log(error));
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false);
            }
        },
        enableReinitialize: true,
        validateOnChange: true,
        validateOnBlur: true,
    });

    useEffect(() => {
        if (selectedAssign) {
            setAgeFrom(selectedAssign.ageFrom ?? null);
            setAgeEnd(selectedAssign.ageEnd ?? null);
            const transferObject = selectedAssign?.exerciseTypes?.map((s) => {
                return {
                    eType: { ...s },
                    lsExercise: s.exercises
                };
            });
            const trans = selectedAssign?.exerciseTypes?.map((s) => {
                return {
                    "exerciseTypeId": s?.id,
                    "exerciseIds": s?.exercises?.map((e) => e.id)
                };
            })
            console.log(trans);
            console.log(transferObject);
            setSelectedList(trans);
            setSelectedClone(transferObject);
        }
    }, [selectedAssign]);

    const handleDeleteItem = (id) => {
        const newList = selectedList.filter((s) => s.exerciseTypeId !== id);
        const newListClone = selectedClone.filter((s) => s.eType.id !== id);

        setSelectedList(newList);
        setSelectedClone(newListClone);
    };

    return (
        <Stack direction={'column'} gap={3} sx={{ width: "80%", margin: "auto", padding: 3, backgroundColor: '#fff', borderRadius: 2, boxShadow: 3 }}>
            <Typography variant='h4' my={2} textAlign="center" sx={{ fontWeight: 'bold' }}>Gán bài tập vào giáo trình</Typography>
            <Divider />
            <form onSubmit={formik.handleSubmit}>
                <Grid container spacing={2} alignItems="center" mt={2}>
                    <Grid item xs={4}>
                        <Typography variant='h6' sx={{ textAlign: 'right', pr: 2 }}>Độ tuổi:</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Grid container spacing={2}>
                            <Grid item xs={4}>
                                <TextField
                                    name='ageFrom'
                                    label="Từ"
                                    type="number"
                                    size="small"
                                    value={formik.values.ageFrom}
                                    onChange={
                                        (e) => {
                                            const value = parseInt(e.target.value);
                                            setAgeFrom(isNaN(value) ? null : value);
                                        }
                                    }
                                    onBlur={formik.handleBlur}
                                    error={formik.touched.ageFrom && Boolean(formik.errors.ageFrom)}
                                    helperText={formik.touched.ageFrom && formik.errors.ageFrom}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={4}>
                                <TextField
                                    name='ageEnd'
                                    label="Đến"
                                    type="number"
                                    size="small"
                                    value={formik.values.ageEnd}
                                    onChange={
                                        (e) => {
                                            const value = parseInt(e.target.value);
                                            setAgeEnd(isNaN(value) ? null : value);
                                        }
                                    }
                                    onBlur={formik.handleBlur}
                                    error={formik.touched.ageEnd && Boolean(formik.errors.ageEnd)}
                                    helperText={formik.touched.ageEnd && formik.errors.ageEnd}
                                    fullWidth
                                />
                            </Grid>
                        </Grid>

                    </Grid>

                    <Grid item xs={4}>
                        <Typography variant='h6' sx={{ textAlign: 'right', pr: 2 }}>Thêm loại bài tập và bài tập</Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <IconButton onClick={handleOpenModal} color="primary" sx={{ backgroundColor: '#f5f7f8', borderRadius: '50%', padding: 1 }}>
                            <AddIcon />
                        </IconButton>
                        {formik.errors.syllabusExercises && (
                            <Typography color="error" variant="body2">
                                {formik.errors.syllabusExercises}
                            </Typography>
                        )}
                    </Grid>
                    <Grid item xs={12}>
                        {selectedClone.length !== 0 && (
                            <Box sx={{ width: '85%', margin: 'auto' }} p={2} borderRadius={2} bgcolor={'#fff8e3'}>
                                <Typography variant='h6' mb={2}>Danh sách loại bài tập và bài tập:</Typography>
                                <Stack direction={'row'}>
                                    <Stack sx={{ width: '90%' }} direction={'column'} gap={2}>
                                        {selectedClone?.map((s, index) => (
                                            <Stack direction={'row'} gap={2} sx={{ width: '100%' }} key={index}>
                                                <Box sx={{ width: "5%" }}>
                                                    <CheckCircleIcon color='success' fontSize='medium' />
                                                </Box>
                                                <Box sx={{ width: '95%' }}>
                                                    <Typography variant='h6'>{`${index + 1}. `}{s.eType.exerciseTypeName}</Typography>
                                                    <Box ml={2}>
                                                        {s?.lsExercise?.map((l, index) => (
                                                            <Typography key={index} variant='body1'>{`${index + 1}. `}{l?.exerciseName}</Typography>
                                                        ))}
                                                    </Box>
                                                </Box>
                                                <Box sx={{ width: '5%' }}>
                                                    <IconButton color='error' onClick={() => handleDeleteItem(s?.eType?.id)}>
                                                        <DeleteIcon />
                                                    </IconButton>
                                                </Box>
                                            </Stack>
                                        ))}
                                    </Stack>
                                    <Box sx={{ width: "10%", display: "flex", alignItems: "end" }}>
                                        <img src='https://cdn-icons-png.freepik.com/256/4295/4295914.png?semt=ais_hybrid'
                                            style={{ width: "100%", objectFit: "cover", objectPosition: "center" }}
                                        />
                                    </Box>
                                </Stack>
                            </Box>
                        )
                            // :
                            //     <Grid item xs={12}>
                            //         <Stack direction={'row'} p={5} borderRadius={3} bgcolor={'#fff8e3'}>
                            //             <Stack sx={{ width: '80%' }} direction={'column'} gap={2}>
                            //                 {selectedAssign?.exerciseTypes?.map((s, index) => (
                            //                     <Stack direction={'row'} gap={2} sx={{ width: '100%' }} key={index}>
                            //                         <Box sx={{ width: "5%" }}>
                            //                             <CheckCircleIcon color='success' fontSize='large' />
                            //                         </Box>
                            //                         <Box key={index} sx={{ width: '95%' }} pt={0.5}>
                            //                             <Typography variant='h5'>{`${index + 1}. `}{s.exerciseTypeName}</Typography>
                            //                             <Box ml={2}>
                            //                                 {s?.exercises?.map((e, index) => (
                            //                                     <Typography key={index} variant='subtitle1'>{`${index + 1}. `}{e?.exerciseName}</Typography>
                            //                                 ))}
                            //                             </Box>
                            //                         </Box>
                            //                     </Stack>
                            //                 ))}
                            //             </Stack>
                            //             <Stack direction={'column'} justifyContent={'space-between'} alignItems={'center'} sx={{ width: "20%" }}>
                            //                 <img src='https://cdn-icons-png.freepik.com/256/4295/4295914.png?semt=ais_hybrid'
                            //                     style={{ width: "60%", objectFit: "cover", objectPosition: "center" }}
                            //                 />
                            //             </Stack>
                            //         </Stack>
                            //     </Grid>
                        }
                    </Grid>
                </Grid>

                <Stack direction="row" justifyContent="end" mt={2}>
                    <Button variant="outlined" color="inherit" sx={{ mr: 2 }} onClick={handleBack}>Trở về</Button>
                    <Button type="submit" variant="contained" color="primary" disabled={loading || !formik.isValid}>
                        {loading ? <LoadingComponent open={loading} /> : "Lưu"}
                    </Button>

                </Stack>
            </form>

            {openModal && (
                <ExerciseAdd
                    openModal={openModal}
                    handleCloseModal={handleCloseModal}
                    exerciseTypes={exerciseTypes}
                    selectedList={selectedList}
                    setSelectedList={setSelectedList}
                    selectedClone={selectedClone}
                    setSelectedClone={setSelectedClone}
                />
            )}
        </Stack>
    );
}
